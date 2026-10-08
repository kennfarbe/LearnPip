// <copyright file="PublicSubmissionService.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.CatalogPackages;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Verwaltet Vorschau, Einreichung und Moderation öffentlicher Fragenfassungen.
/// </summary>
/// <param name="db">Der Datenbankkontext.</param>
/// <param name="configuration">Die ausdrückliche administrative Freigabe.</param>
public sealed class PublicSubmissionService(LearnPipDbContext db, IConfiguration configuration)
{
    /// <summary>
    /// Die bei öffentlichen Einreichungen unterstützten Inhaltslizenzen.
    /// </summary>
    public static readonly string[] Licenses = ["CC BY 4.0", "CC BY-SA 4.0", "CC0 1.0", "CC-BY-4.0", "CC-BY-SA-4.0", "CC0-1.0", "DL-DE/BY-2.0", "dl-de/by-2-0"];

    /// <summary>
    /// Erstellt eine Veröffentlichungsvorschau mit Bestätigungstoken.
    /// </summary>
    /// <param name="questionId">Die Kennung der Frage.</param>
    /// <param name="number">Die Nummer der Fragenfassung.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<PublicPreview?> PreviewAsync(
        Guid questionId,
        int number,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var versionId = await db.QuestionVersions.AsNoTracking().Where(version =>
                version.QuestionId == questionId && version.VersionNumber == number &&
                version.Question.OwnerAccountId == accountId && version.Question.DeletedAtUtc == null)
            .Select(version => (Guid?)version.Id).SingleOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue)
        {
            return null;
        }

        var version = await QuestionEndpoints.LoadVersion(db, versionId.Value, cancellationToken);
        if (version == null)
        {
            return null;
        }

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.QuestionVersions.FromSqlInterpolated(
                $"SELECT * FROM \"QuestionVersions\" WHERE \"Id\" = {versionId.Value} FOR UPDATE")
            .SingleAsync(cancellationToken);
        await db.PublicSubmissionPreviews.Where(item => item.QuestionVersionId == versionId)
            .ExecuteDeleteAsync(cancellationToken);
        db.PublicSubmissionPreviews.Add(new PublicSubmissionPreview
        {
            QuestionVersionId = versionId.Value,
            AccountId = accountId,
            TokenHash = SessionAuthentication.Hash(token),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var hasImages = version.Prompt.Concat(version.Explanation)
            .Concat(version.Answers.SelectMany(answer => answer.Blocks))
            .Any(block => block.Kind == "image");
        var report = await CatalogRightsReport.ForVersion(questionId, version, db, cancellationToken);
        var enabled = configuration.GetValue<bool>("CatalogPackages:CommunityExportEnabled");
        var evidence = await CatalogRightsReport.Evidence(questionId, version, db, cancellationToken);
        return new PublicPreview(version, token, hasImages) { Rights = evidence == null ? null : JsonSerializer.Deserialize<JsonElement>(evidence), RightsReport = report, CommunityEnabled = enabled, CommunityEligible = enabled && report.Count == 0 };
    }

    /// <summary>
    /// Reicht eine bestätigte Fragenfassung zur öffentlichen Moderation ein.
    /// </summary>
    /// <param name="questionId">Die Kennung der Frage.</param>
    /// <param name="number">Die Nummer der Fragenfassung.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="input">Die zu prüfenden Eingabedaten.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<string> SubmitAsync(
        Guid questionId,
        int number,
        Guid accountId,
        PublicSubmissionInput input,
        CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("CatalogPackages:CommunityExportEnabled"))
        {
            return "rights_blocked";
        }

        if (input.PreviewToken is not { Length: 43 } ||
            !Licenses.Contains(input.LicenseChoice, StringComparer.Ordinal) ||
            string.IsNullOrWhiteSpace(input.AuthorAttribution) ||
            input.AuthorAttribution.Trim().Length > 120 || !input.RightsConfirmed ||
            input.AgeDeclaration is not ("adult" or "minor"))
        {
            return "invalid";
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({questionId.ToString()}))", cancellationToken);
        var version = await db.QuestionVersions.FromSqlInterpolated(
                $"SELECT * FROM \"QuestionVersions\" WHERE \"QuestionId\" = {questionId} AND \"VersionNumber\" = {number} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (version == null || !await db.Questions.AnyAsync(
            question =>
                question.Id == questionId && question.OwnerAccountId == accountId &&
                question.DeletedAtUtc == null,
            cancellationToken))
        {
            return "missing";
        }

        if (version.Visibility != "private" || string.IsNullOrWhiteSpace(version.Source))
        {
            return "invalid";
        }

        var content = (await QuestionEndpoints.LoadVersion(db, version.Id, cancellationToken))!;
        if ((await CatalogRightsReport.ForVersion(questionId, content, db, cancellationToken)).Count != 0)
        {
            return "rights_blocked";
        }

        var rights = await db.QuestionRights.SingleAsync(item => item.QuestionId == questionId, cancellationToken);
        var evidence = (await CatalogRightsReport.Evidence(questionId, content, db, cancellationToken))!;
        var declaredLicense = JsonSerializer.Deserialize<JsonElement>(evidence).GetProperty("license").GetProperty("id").GetString()!;
        if (CatalogRightsReport.Normalize(declaredLicense) != CatalogRightsReport.Normalize(input.LicenseChoice))
        {
            return "rights_blocked";
        }

        await db.Accounts.Where(item => item.Id == accountId && item.AgeBand == "unknown")
            .ExecuteUpdateAsync(
            setters => setters.SetProperty(
                item => item.AgeBand,
                input.AgeDeclaration),
            cancellationToken);
        var ageBand = await db.Accounts.Where(item => item.Id == accountId)
            .Select(item => item.AgeBand).SingleAsync(cancellationToken);
        if (ageBand != input.AgeDeclaration)
        {
            return "invalid";
        }

        var preview = await db.PublicSubmissionPreviews.SingleOrDefaultAsync(
            item =>
            item.QuestionVersionId == version.Id && item.AccountId == accountId,
            cancellationToken);
        if (preview == null || preview.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
            !ValidToken(input.PreviewToken, preview.TokenHash))
        {
            return "invalid_preview";
        }

        var hasImages = await db.QuestionContentBlocks.AnyAsync(
            block => block.Kind == "image" &&
            (block.QuestionVersionId == version.Id || (block.AnswerOption != null &&
             block.AnswerOption.QuestionVersionId == version.Id)),
            cancellationToken);
        if (hasImages && !input.ImageRightsConfirmed)
        {
            return "invalid";
        }

        var submission = await db.PublicSubmissions.SingleOrDefaultAsync(
            item =>
            item.QuestionVersionId == version.Id,
            cancellationToken);
        if (submission?.Status is "pending" or "minor_hold" or "approved")
        {
            return "conflict";
        }

        if (submission == null)
        {
            submission = new PublicSubmission { QuestionVersionId = version.Id, AccountId = accountId };
            db.PublicSubmissions.Add(submission);
        }

        version.License = declaredLicense;
        var previousHash = await CatalogRightsEndpoints.Fingerprint(CatalogRightsEndpoints.VersionContent(content), db, cancellationToken);
        var publishedHash = await CatalogRightsEndpoints.Fingerprint(CatalogRightsEndpoints.VersionContent(content) with { License = declaredLicense }, db, cancellationToken);
        if (previousHash != publishedHash)
        {
            try
            {
                CatalogRightsEndpoints.Remember(rights, publishedHash, evidence, DateTimeOffset.UtcNow);
            }
            catch (InvalidDataException)
            {
                return "rights_blocked";
            }
        }

        submission.RightsJson = evidence;
        version.AuthorAttribution = input.AuthorAttribution.Trim();
        submission.LicenseChoice = declaredLicense;
        submission.AuthorAttribution = version.AuthorAttribution;
        submission.RightsConfirmed = true;
        submission.ImageRightsConfirmed = input.ImageRightsConfirmed;
        submission.AgeDeclaration = input.AgeDeclaration;
        submission.GuardianApprovedByAccountId = null;
        submission.GuardianApprovedAtUtc = null;
        submission.Status = input.AgeDeclaration == "minor" ? "minor_hold" : "pending";
        submission.SubmittedAtUtc = DateTimeOffset.UtcNow;
        submission.ReviewedAtUtc = null;
        submission.ReviewedByAccountId = null;
        submission.ReviewNote = null;
        db.PublicSubmissionPreviews.Remove(preview);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return submission.Status;
    }

    /// <summary>
    /// Prüft und protokolliert die Moderationsentscheidung einer öffentlichen Einreichung.
    /// </summary>
    /// <param name="versionId">Die Kennung der Fragenfassung.</param>
    /// <param name="moderatorId">Die Kennung des prüfenden Moderators.</param>
    /// <param name="input">Die zu prüfenden Eingabedaten.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<string> ReviewAsync(
        Guid versionId,
        Guid moderatorId,
        PublicReviewInput input,
        CancellationToken cancellationToken)
    {
        if (input.Decision is not ("approve" or "reject" or "changes_requested") ||
            input.Note == null || input.Note.Trim().Length > 1000 ||
            (input.Decision != "approve" && string.IsNullOrWhiteSpace(input.Note)) ||
            (input.Decision == "approve" && !(input.CorrectnessChecked &&
                input.ImageRightsChecked && input.PersonalDataChecked && input.DuplicateChecked)))
        {
            return "invalid";
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var version = await db.QuestionVersions.FromSqlInterpolated(
                $"SELECT * FROM \"QuestionVersions\" WHERE \"Id\" = {versionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (version == null)
        {
            return "missing";
        }

        var submission = await db.PublicSubmissions.SingleOrDefaultAsync(
            item =>
            item.QuestionVersionId == versionId,
            cancellationToken);
        if (submission == null || submission.Status is not ("pending" or "minor_hold") ||
            submission.AccountId == moderatorId)
        {
            return "conflict";
        }

        // A guardian approves this exact version; the active link must still exist at publication.
        if (submission.Status == "minor_hold" && input.Decision == "approve" &&
            (submission.GuardianApprovedByAccountId == null ||
             submission.GuardianApprovedAtUtc == null ||
             !await db.FamilyLinks.AnyAsync(
            link => link.ChildAccountId == submission.AccountId &&
                 link.ParentAccountId == submission.GuardianApprovedByAccountId &&
                 link.Status == "active" && link.RevokedAtUtc == null &&
                 link.VerifiedByAccountId != null && link.ActivatedAtUtc != null,
            cancellationToken)))
        {
            return "minor_hold";
        }

        if (version.Visibility != "private" || !submission.RightsConfirmed ||
            string.IsNullOrWhiteSpace(version.Source) ||
            version.License != submission.LicenseChoice ||
            version.AuthorAttribution != submission.AuthorAttribution)
        {
            return "conflict";
        }

        if (input.Decision == "approve" && (!configuration.GetValue<bool>("CatalogPackages:CommunityExportEnabled") ||
            (await CatalogRightsReport.ForVersion(version.QuestionId, (await QuestionEndpoints.LoadVersion(db, version.Id, cancellationToken))!, db, cancellationToken)).Count != 0))
        {
            return "rights_blocked";
        }

        var now = DateTimeOffset.UtcNow;
        version.Visibility = input.Decision == "approve" ? "public" : "private";
        submission.Status = input.Decision switch
        {
            "approve" => "approved",
            "reject" => "rejected",
            _ => "changes_requested",
        };
        submission.ReviewedByAccountId = moderatorId;
        submission.ReviewedAtUtc = now;
        submission.ReviewNote = input.Note.Trim();
        db.PublicSubmissionReviews.Add(new PublicSubmissionReview
        {
            QuestionVersionId = versionId,
            ModeratorAccountId = moderatorId,
            Decision = input.Decision,
            CorrectnessChecked = input.CorrectnessChecked,
            ImageRightsChecked = input.ImageRightsChecked,
            PersonalDataChecked = input.PersonalDataChecked,
            DuplicateChecked = input.DuplicateChecked,
            Note = input.Note.Trim(),
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return submission.Status;
    }

    private static bool ValidToken(
        string token,
        string hash)
    {
        if (token.Any(character => !char.IsAsciiLetterOrDigit(character) &&
            character is not ('-' or '_')))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(hash),
            Convert.FromHexString(SessionAuthentication.Hash(token)));
    }
}
