// <copyright file="TranslationEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Registriert HTTP-Endpunkte für die Übersetzungen von Fragenfassungen.
/// </summary>
public static class TranslationEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Registriert HTTP-Endpunkte für die Übersetzungen von Fragenfassungen.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        var versions = app.MapGroup("/api/v1/questions/{id:guid}/versions/{number:int}/translations")
            .WithTags("Question translations").RequireAuthorization(ApiPolicies.ActiveAccount);
        versions.MapGet("/{language}", Read);
        versions.MapGet("/history/{language}", History);
        versions.MapPost(
            "/drafts",
            CreateDraft)
            .WithMetadata(new RequestSizeLimitAttribute(70 * 1024));
        versions.MapPost(
            "/suggest",
            Suggest)
            .WithMetadata(new RequestSizeLimitAttribute(4 * 1024));
        versions.MapPost("/{translationId:guid}/approve", Approve);
        versions.MapPost(
            "/{translationId:guid}/reports",
            Report)
            .WithMetadata(new RequestSizeLimitAttribute(4 * 1024));
        return app;
    }

    /// <summary>
    /// Prüft, ob der Sprachcode unterstützt wird.
    /// </summary>
    /// <param name="language">Der gewünschte Sprachcode.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool ValidLanguage(string? language) => language is "de" or "en";

    /// <summary>
    /// Prüft, ob eine Übersetzung zur Struktur der ursprünglichen Fragenfassung passt.
    /// </summary>
    /// <param name="payload">Die übersetzten Inhaltsblöcke.</param>
    /// <param name="original">Die ursprüngliche veröffentlichte Fragenfassung.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool Valid(
        TranslationPayload? payload,
        PublishedQuestionVersion original)
    {
        if (payload?.Prompt == null || payload.Explanation == null || payload.Answers == null ||
            payload.Answers.Count != original.Answers.Count ||
            payload.Answers.Any(answer => answer == null || answer.Blocks == null) ||
            payload.Answers.Select(answer => answer.OptionId).Distinct().Count() != original.Answers.Count ||
            !SameBlocks(payload.Prompt, original.Prompt) ||
            !SameBlocks(payload.Explanation, original.Explanation))
        {
            return false;
        }

        return original.Answers.All(answer =>
        {
            var translated = payload.Answers.SingleOrDefault(item => item.OptionId == answer.Id);
            return translated != null && SameBlocks(translated.Blocks, answer.Blocks);
        });
    }

    /// <summary>
    /// Überträgt übersetzte Inhaltsblöcke auf eine veröffentlichte Fragenfassung.
    /// </summary>
    /// <param name="original">Die ursprüngliche veröffentlichte Fragenfassung.</param>
    /// <param name="payload">Die übersetzten Inhaltsblöcke.</param>
    /// <param name="language">Der gewünschte Sprachcode.</param>
    /// <param name="source">Die Herkunft der Inhalte.</param>
    /// <param name="license">Die Inhaltslizenz.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static PublishedQuestionVersion Apply(
        PublishedQuestionVersion original,
        TranslationPayload payload,
        string language,
        string source,
        string license) =>
        original with
        {
            Language = language,
            Source = source,
            License = license,
            Prompt = Convert(payload.Prompt),
            Explanation = Convert(payload.Explanation),
            Answers = original.Answers.Select(answer => answer with
            {
                Blocks = Convert(payload.Answers.Single(item => item.OptionId == answer.Id).Blocks),
            }).ToArray(),
        };

    /// <summary>
    /// Erstellt die Übersetzungsstruktur einer ursprünglichen Fragenfassung.
    /// </summary>
    /// <param name="original">Die ursprüngliche veröffentlichte Fragenfassung.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static TranslationPayload FromOriginal(PublishedQuestionVersion original) =>
        new(
        original.Prompt.Select(FromBlock).ToArray(),
        original.Explanation.Select(FromBlock).ToArray(),
        original.Answers.Select(answer => new LocalizedAnswer(
                answer.Id,
                answer.Blocks.Select(FromBlock).ToArray())).ToArray());

    /// <summary>
    /// Lädt eine freigegebene Übersetzung oder verwendet die ursprüngliche Fragenfassung.
    /// </summary>
    /// <param name="original">Die ursprüngliche veröffentlichte Fragenfassung.</param>
    /// <param name="translationId">Die Kennung der Übersetzung, sofern vorhanden.</param>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="ct">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static async Task<PublishedQuestionVersion> Localize(
        PublishedQuestionVersion original,
        Guid? translationId,
        LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!translationId.HasValue)
        {
            return original;
        }

        var row = await db.QuestionTranslations.AsNoTracking().SingleOrDefaultAsync(
            item =>
            item.Id == translationId && item.QuestionVersionId == original.Id &&
            item.Status == "approved",
            ct);
        var payload = row == null ? null : JsonSerializer.Deserialize<TranslationPayload>(row.PayloadJson, Json);
        return row != null && Valid(payload, original) ? Apply(
            original,
            payload!,
            row.Language,
            row.Source,
            row.License) : original;
    }

    private static bool SameBlocks(
        IReadOnlyList<LocalizedBlock>? translated,
        IReadOnlyList<ContentBlockOutput> original)
    {
        if (translated == null || translated.Count != original.Count)
        {
            return false;
        }

        for (var index = 0; index < original.Count; index++)
        {
            var block = translated[index];
            var source = original[index];
            if (block == null || block.Kind != source.Kind || block.MediaId != source.MediaId)
            {
                return false;
            }

            if (block.Kind == "text")
            {
                if (block.MediaId != null || block.AltText != null ||
                    string.IsNullOrWhiteSpace(block.Text) || block.Text.Length > 4000)
                {
                    return false;
                }
            }
            else if (block.Kind == "image")
            {
                if (block.MediaId == null || block.Text != null ||
                    string.IsNullOrWhiteSpace(block.AltText) || block.AltText.Length > 300)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static ContentBlockOutput[] Convert(IReadOnlyList<LocalizedBlock> blocks) =>
        blocks.Select(block => new ContentBlockOutput(
            block.Kind,
            block.Text,
            block.MediaId,
            block.AltText)).ToArray();

    private static LocalizedBlock FromBlock(ContentBlockOutput block) =>
        new(block.Kind, block.Text, block.MediaId, block.AltText);

    private static async Task<PublishedQuestionVersion?> Readable(
        Guid id,
        int number,
        Guid accountId,
        LearnPipDbContext db,
        CancellationToken ct)
    {
        var versionId = await QuestionAccess.ReadableVersions(db, accountId).AsNoTracking()
            .Where(version => version.QuestionId == id && version.VersionNumber == number)
            .Select(version => (Guid?)version.Id).SingleOrDefaultAsync(ct);
        return versionId.HasValue ? await QuestionEndpoints.LoadVersion(db, versionId.Value, ct) : null;
    }

    private static async Task<PublishedQuestionVersion?> Owned(
        Guid id,
        int number,
        Guid accountId,
        LearnPipDbContext db,
        CancellationToken ct)
    {
        var versionId = await db.QuestionVersions.AsNoTracking().Where(version =>
            version.QuestionId == id && version.VersionNumber == number &&
            version.Question.OwnerAccountId == accountId && version.Question.DeletedAtUtc == null)
            .Select(version => (Guid?)version.Id).SingleOrDefaultAsync(ct);
        return versionId.HasValue ? await QuestionEndpoints.LoadVersion(db, versionId.Value, ct) : null;
    }

    private static async Task<IResult> Read(
        Guid id,
        int number,
        string language,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        if (!ValidLanguage(language))
        {
            return Results.BadRequest();
        }

        var original = await Readable(id, number, accountId, db, ct);
        if (original == null)
        {
            return Results.NotFound();
        }

        if (language == original.Language)
        {
            return Results.Ok(new ApiResponse<TranslationView>(new TranslationView(
                        null,
                        0,
                        language,
                        original.Source,
                        original.License,
                        "original",
                        false,
                        FromOriginal(original))));
        }

        var row = await db.QuestionTranslations.AsNoTracking().Where(item =>
            item.QuestionVersionId == original.Id && item.Language == language &&
            item.Status == "approved").OrderByDescending(item => item.Revision).FirstOrDefaultAsync(ct);
        return Results.Ok(new ApiResponse<TranslationView>(row == null ?
            new TranslationView(
                    null,
                    0,
                    language,
                    original.Source,
                    original.License,
                    "original",
                    true,
                    FromOriginal(original)) :
            new TranslationView(
                    row.Id,
                    row.Revision,
                    language,
                    row.Source,
                    row.License,
                    row.Provenance,
                    false,
                    JsonSerializer.Deserialize<TranslationPayload>(row.PayloadJson, Json)!)));
    }

    private static async Task<IResult> History(
        Guid id,
        int number,
        string language,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        if (!ValidLanguage(language))
        {
            return Results.BadRequest();
        }

        var original = await Owned(id, number, accountId, db, ct);
        if (original == null)
        {
            return Results.NotFound();
        }

        var history = await db.QuestionTranslations.AsNoTracking().Where(item =>
            item.QuestionVersionId == original.Id && item.Language == language)
            .OrderByDescending(item => item.Revision).ToListAsync(ct);
        var ids = history.Select(item => item.Id).ToArray();
        var reports = await db.TranslationReports.AsNoTracking().Where(item =>
            ids.Contains(item.QuestionTranslationId))
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new { item.QuestionTranslationId, item.Details, item.CreatedAtUtc })
            .ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(history.Select(item => new
        {
            item.Id,
            item.Revision,
            item.Status,
            item.Source,
            item.License,
            item.Provenance,
            item.CreatedAtUtc,
            item.ApprovedAtUtc,
            Reports = reports.Where(report => report.QuestionTranslationId == item.Id)
                .Select(report => new { report.Details, report.CreatedAtUtc }).ToArray(),
            Payload = JsonSerializer.Deserialize<TranslationPayload>(item.PayloadJson, Json),
        }).ToArray()));
    }

    private static async Task<IResult> CreateDraft(
        Guid id,
        int number,
        TranslationDraftInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var original = await Owned(id, number, accountId, db, ct);
        if (original == null)
        {
            return Results.NotFound();
        }

        if (!ValidLanguage(input.Language) || input.Language == original.Language ||
            input.Provenance is not ("manual" or "ai-assisted") ||
            input.Source is null or { Length: > 500 } ||
            input.License is null or { Length: > 120 } || !Valid(input.Payload, original))
        {
            return Results.BadRequest(new { error = "Complete every translated block and preserve answer IDs." });
        }

        var json = JsonSerializer.Serialize(input.Payload, Json);
        if (json.Length > 65536)
        {
            return Results.BadRequest();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.QuestionVersions.FromSqlInterpolated(
            $"SELECT * FROM \"QuestionVersions\" WHERE \"Id\" = {original.Id} FOR UPDATE")
            .SingleAsync(ct);
        var revision = (await db.QuestionTranslations.Where(item =>
            item.QuestionVersionId == original.Id && item.Language == input.Language)
            .MaxAsync(item => (int?)item.Revision, ct) ?? 0) + 1;
        var row = new QuestionTranslation
        {
            QuestionVersionId = original.Id,
            Language = input.Language,
            Revision = revision,
            PayloadJson = json,
            Source = input.Source.Trim(),
            License = input.License.Trim(),
            Provenance = input.Provenance,
            CreatedByAccountId = accountId,
        };
        db.QuestionTranslations.Add(row);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created(
            $"/api/v1/questions/{id}/versions/{number}/translations/history/{input.Language}",
            new ApiResponse<TranslationDraftCreated>(new TranslationDraftCreated(
                    row.Id,
                    row.Revision,
                    row.Status)));
    }

    private static async Task<IResult> Approve(
        Guid id,
        int number,
        Guid translationId,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var row = await db.QuestionTranslations.SingleOrDefaultAsync(
            item => item.Id == translationId &&
            item.QuestionVersion.QuestionId == id && item.QuestionVersion.VersionNumber == number &&
            item.QuestionVersion.Question.OwnerAccountId == accountId &&
            item.QuestionVersion.Question.DeletedAtUtc == null,
            ct);
        if (row == null)
        {
            return Results.NotFound();
        }

        if (row.Status != "draft" || string.IsNullOrWhiteSpace(row.Source) ||
            string.IsNullOrWhiteSpace(row.License))
        {
            return Results.Conflict(new
            {
                error = "Translation must be a draft with source and license before approval.",
            });
        }

        if (await db.QuestionTranslations.AnyAsync(
            item => item.QuestionVersionId == row.QuestionVersionId &&
            item.Language == row.Language && item.Status == "approved" &&
            item.Revision > row.Revision,
            ct))
        {
            return Results.Conflict(new
            { error = "A newer revision is already approved.", });
        }

        var original = await QuestionEndpoints.LoadVersion(db, row.QuestionVersionId, ct);
        var payload = JsonSerializer.Deserialize<TranslationPayload>(row.PayloadJson, Json);
        if (original == null || !Valid(payload, original))
        {
            return Results.Conflict();
        }

        row.Status = "approved";
        row.ApprovedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new ApiResponse<object>(new { row.Id, row.Revision, row.Status }));
    }

    private static async Task<IResult> Report(
        Guid id,
        int number,
        Guid translationId,
        TranslationReportInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(input.Details) || input.Details.Length > 2000)
        {
            return Results.BadRequest();
        }

        var original = await Readable(id, number, accountId, db, ct);
        if (original == null || !await db.QuestionTranslations.AnyAsync(
            item =>
            item.Id == translationId && item.QuestionVersionId == original.Id &&
            item.Status == "approved",
            ct))
        {
            return Results.NotFound();
        }

        var report = new TranslationReport
        {
            QuestionTranslationId = translationId,
            AccountId = accountId,
            Details = input.Details.Trim(),
        };
        db.TranslationReports.Add(report);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/v1/questions/{id}/versions/{number}/translations/{translationId}/reports",
            new ApiResponse<object>(new { report.Id, report.CreatedAtUtc }));
    }

    private static async Task<IResult> Suggest(
        Guid id,
        int number,
        TranslationSuggestionInput input,
        LearnPipDbContext db,
        IConfiguration config,
        AiGateway gateway,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var original = await Owned(id, number, accountId, db, ct);
        if (original == null)
        {
            return Results.NotFound();
        }

        if (!ValidLanguage(input.Language) || input.Language == original.Language ||
            input.Mode == "off" || !AiPolicy.Modes.Contains(input.Mode, StringComparer.Ordinal))
        {
            return Results.BadRequest();
        }

        if (!input.Confirmed || input.DisclosureVersion != AiPolicy.DisclosureVersion)
        {
            return Results.BadRequest(new { error = "Confirm the selected provider and text transfer." });
        }

        var keyRow = input.Mode == "user-key" ? await db.UserAiCredentials.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId, ct) : null;
        var info = AiPolicy.Describe(input.Mode, config, keyRow != null);
        if (!info.Available)
        {
            return Results.Conflict(new { error = "Selected AI mode unavailable." });
        }

        var source = JsonSerializer.Serialize(FromOriginal(original), Json);
        var prompt = $"Translate this educational question from {original.Language} to {input.Language}. " +
            "Return ONLY a JSON object with camelCase fields prompt, explanation and answers " +
            "in the identical structure and order as the source JSON. Preserve every kind, " +
            "mediaId and optionId exactly; translate text and image altText only. " +
            "Do not invent facts. Source JSON: " + source;
        if (Encoding.UTF8.GetByteCount(prompt) > info.MaxInputBytes)
        {
            return Results.Problem("Source text exceeds the provider input limit.", statusCode: 413);
        }

        string? key = null;
        if (keyRow != null)
        {
            try
            {
                key = AiKeyVault.Open(keyRow.Ciphertext, accountId, config);
            }
            catch (Exception error) when (error is FormatException or CryptographicException)
            {
                return Results.Conflict(new { error = "Stored key unavailable; replace it." });
            }
        }

        if (!await AiEndpoints.Reserve(db, accountId, input.Mode, info.DailyQuota, ct))
        {
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        try
        {
            var response = await gateway.Resolve(input.Mode, config, key).TranslateAsync(prompt, ct);
            var payload = JsonSerializer.Deserialize<TranslationPayload>(response, Json);
            return !Valid(payload, original) ? Results.UnprocessableEntity(new
            {
                error = "Provider returned an incomplete translation; edit manually.",
            }) : Results.Ok(new ApiResponse<TranslationPayload>(payload!));
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException or
            JsonException or OperationCanceledException or KeyNotFoundException or InvalidOperationException)
        {
            return Results.Problem("Translation provider failed.", statusCode: 502);
        }
    }
}
