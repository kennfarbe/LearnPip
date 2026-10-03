// <copyright file="PublicSubmissionService.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed class PublicSubmissionService(LearnPipDbContext db)
{
    public static readonly string[] Licenses = ["CC BY 4.0", "CC BY-SA 4.0", "CC0 1.0"];

    public async Task<PublicPreview?> PreviewAsync(Guid questionId, int number, Guid accountId,
        CancellationToken cancellationToken)
    {
        var versionId = await db.QuestionVersions.AsNoTracking().Where(version =>
                version.QuestionId == questionId && version.VersionNumber == number &&
                version.Question.OwnerAccountId == accountId && version.Question.DeletedAtUtc == null)
            .Select(version => (Guid?)version.Id).SingleOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue) return null;
        var version = await QuestionEndpoints.LoadVersion(db, versionId.Value, cancellationToken);
        if (version == null) return null;
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
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15)
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var hasImages = version.Prompt.Concat(version.Explanation)
            .Concat(version.Answers.SelectMany(answer => answer.Blocks))
            .Any(block => block.Kind == "image");
        return new PublicPreview(version, token, hasImages);
    }

    public async Task<string> SubmitAsync(Guid questionId, int number, Guid accountId,
        PublicSubmissionInput input, CancellationToken cancellationToken)
    {
        if (input.PreviewToken is not { Length: 43 } ||
            !Licenses.Contains(input.LicenseChoice, StringComparer.Ordinal) ||
            string.IsNullOrWhiteSpace(input.AuthorAttribution) ||
            input.AuthorAttribution.Trim().Length > 120 || !input.RightsConfirmed ||
            input.AgeDeclaration is not ("adult" or "minor")) return "invalid";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var version = await db.QuestionVersions.FromSqlInterpolated(
                $"SELECT * FROM \"QuestionVersions\" WHERE \"QuestionId\" = {questionId} AND \"VersionNumber\" = {number} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (version == null || !await db.Questions.AnyAsync(question =>
                question.Id == questionId && question.OwnerAccountId == accountId &&
                question.DeletedAtUtc == null, cancellationToken)) return "missing";
        if (version.Visibility != "private" || string.IsNullOrWhiteSpace(version.Source)) return "invalid";
        await db.Accounts.Where(item => item.Id == accountId && item.AgeBand == "unknown")
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.AgeBand,
                input.AgeDeclaration), cancellationToken);
        var ageBand = await db.Accounts.Where(item => item.Id == accountId)
            .Select(item => item.AgeBand).SingleAsync(cancellationToken);
        if (ageBand != input.AgeDeclaration) return "invalid";
        var preview = await db.PublicSubmissionPreviews.SingleOrDefaultAsync(item =>
            item.QuestionVersionId == version.Id && item.AccountId == accountId,
            cancellationToken);
        if (preview == null || preview.ExpiresAtUtc <= DateTimeOffset.UtcNow ||
            !ValidToken(input.PreviewToken, preview.TokenHash)) return "invalid_preview";
        var hasImages = await db.QuestionContentBlocks.AnyAsync(block => block.Kind == "image" &&
            (block.QuestionVersionId == version.Id || block.AnswerOption != null &&
             block.AnswerOption.QuestionVersionId == version.Id), cancellationToken);
        if (hasImages && !input.ImageRightsConfirmed) return "invalid";
        var submission = await db.PublicSubmissions.SingleOrDefaultAsync(item =>
            item.QuestionVersionId == version.Id, cancellationToken);
        if (submission?.Status is "pending" or "minor_hold" or "approved") return "conflict";
        if (submission == null)
        {
            submission = new PublicSubmission { QuestionVersionId = version.Id, AccountId = accountId };
            db.PublicSubmissions.Add(submission);
        }
        version.License = input.LicenseChoice;
        version.AuthorAttribution = input.AuthorAttribution.Trim();
        submission.LicenseChoice = input.LicenseChoice;
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

    public async Task<string> ReviewAsync(Guid versionId, Guid moderatorId,
        PublicReviewInput input, CancellationToken cancellationToken)
    {
        if (input.Decision is not ("approve" or "reject" or "changes_requested") ||
            input.Note == null || input.Note.Trim().Length > 1000 ||
            input.Decision != "approve" && string.IsNullOrWhiteSpace(input.Note) ||
            input.Decision == "approve" && !(input.CorrectnessChecked &&
                input.ImageRightsChecked && input.PersonalDataChecked && input.DuplicateChecked))
            return "invalid";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var version = await db.QuestionVersions.FromSqlInterpolated(
                $"SELECT * FROM \"QuestionVersions\" WHERE \"Id\" = {versionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (version == null) return "missing";
        var submission = await db.PublicSubmissions.SingleOrDefaultAsync(item =>
            item.QuestionVersionId == versionId, cancellationToken);
        if (submission == null || submission.Status is not ("pending" or "minor_hold") ||
            submission.AccountId == moderatorId) return "conflict";
        // A guardian approves this exact version; the active link must still exist at publication.
        if (submission.Status == "minor_hold" && input.Decision == "approve" &&
            (submission.GuardianApprovedByAccountId == null ||
             submission.GuardianApprovedAtUtc == null ||
             !await db.FamilyLinks.AnyAsync(link => link.ChildAccountId == submission.AccountId &&
                 link.ParentAccountId == submission.GuardianApprovedByAccountId &&
                 link.Status == "active" && link.RevokedAtUtc == null &&
                 link.VerifiedByAccountId != null && link.ActivatedAtUtc != null,
                 cancellationToken))) return "minor_hold";
        if (version.Visibility != "private" || !submission.RightsConfirmed ||
            string.IsNullOrWhiteSpace(version.Source) ||
            version.License != submission.LicenseChoice ||
            version.AuthorAttribution != submission.AuthorAttribution) return "conflict";
        var now = DateTimeOffset.UtcNow;
        version.Visibility = input.Decision == "approve" ? "public" : "private";
        submission.Status = input.Decision switch
        {
            "approve" => "approved",
            "reject" => "rejected",
            _ => "changes_requested"
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
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return submission.Status;
    }

    private static bool ValidToken(string token, string hash)
    {
        if (token.Any(character => !char.IsAsciiLetterOrDigit(character) &&
            character is not ('-' or '_'))) return false;
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(hash), Convert.FromHexString(SessionAuthentication.Hash(token)));
    }
}
