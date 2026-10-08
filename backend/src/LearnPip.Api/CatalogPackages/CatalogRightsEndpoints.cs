// <copyright file="CatalogRightsEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Erfasst Rechte pro Frage und Bild ohne automatische Lizenzzuweisung.</summary>
public static class CatalogRightsEndpoints
{
    /// <summary>Registriert ausschließlich eigentümergebundene Rechteangaben.</summary>
    /// <param name="app">Der Routen-Builder.</param>
    /// <returns>Die ergänzten Routen.</returns>
    public static IEndpointRouteBuilder MapCatalogRightsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog-rights").RequireAuthorization(ApiPolicies.ActiveAccount);
        group.MapGet("/{id:guid}", Read).RequireQuestionPermissions("readOwn");
        group.MapPut("/{id:guid}", Save).RequireQuestionPermissions("readOwn", "editOwn").RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(256 * 1024));
        return app;
    }

    /// <summary>Lädt den tatsächlich exportierten Inhaltsstand.</summary>
    /// <param name="question">Die eigene Frage mit Entwurf und Fassungen.</param>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Die gespeicherten Inhaltsblöcke.</returns>
    public static async Task<QuestionPublishRequest> Content(Question question, LearnPipDbContext db, CancellationToken ct)
    {
        if (question.Draft != null)
        {
            var content = JsonSerializer.Deserialize<QuestionPublishRequest>(question.Draft.PayloadJson);
            if (content?.Prompt == null || content.Explanation == null || content.Answers == null)
            {
                throw new InvalidDataException("Der Entwurf ist unvollständig. Bitte zuerst die Frage speichern.");
            }

            return content;
        }

        var latest = question.Versions.MaxBy(version => version.VersionNumber)
            ?? throw new InvalidDataException("Die Frage ist unvollständig.");
        var version = (await QuestionEndpoints.LoadVersion(db, latest.Id, ct))!;
        return VersionContent(version);
    }

    /// <summary>Konvertiert genau eine unveränderliche Inhaltsfassung ohne Entwurfsbeimischung.</summary>
    /// <param name="version">Die tatsächlich geprüfte Fassung.</param>
    /// <returns>Die vollständigen Inhaltsblöcke dieser Fassung.</returns>
    public static QuestionPublishRequest VersionContent(PublishedQuestionVersion version)
    {
        static ContentBlockInput[] Convert(IReadOnlyList<ContentBlockOutput> blocks) => blocks.Select(block => new ContentBlockInput(block.Kind, block.Text, block.MediaId)).ToArray();
        return new QuestionPublishRequest(version.SelectionMode, version.Subject, version.Topic, version.Language, version.Source, version.License, Convert(version.Prompt), Convert(version.Explanation), version.Answers.Select(answer => new AnswerInput(answer.IsCorrect, Convert(answer.Blocks))).ToArray());
    }

    /// <summary>Bindet Nachweise auch an Änderungen von Bildbytes und Alternativtexten.</summary>
    /// <param name="content">Der exportierte Inhaltsstand.</param>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Ein reproduzierbarer Inhaltsfingerabdruck.</returns>
    public static async Task<string> Fingerprint(QuestionPublishRequest content, LearnPipDbContext db, CancellationToken ct)
    {
        var ids = ImageIds(content);
        var images = await db.MediaAssets.AsNoTracking().Where(image => ids.Contains(image.Id))
            .OrderBy(image => image.Id).Select(image => new { image.Id, image.AltText, image.MediaType }).ToListAsync(ct);
        var blobs = await db.MediaBlobs.AsNoTracking().Where(blob => ids.Contains(blob.MediaAssetId))
            .OrderBy(blob => blob.MediaAssetId).Select(blob => new { blob.MediaAssetId, blob.Data }).ToListAsync(ct);
        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Content = content,
            Images = images,
            Blobs = blobs.Select(blob => new { blob.MediaAssetId, Hash = Convert.ToHexStringLower(SHA256.HashData(blob.Data)) }),
        })));
    }

    /// <summary>Liefert eindeutige Medienkennungen aller Inhaltsabschnitte.</summary>
    /// <param name="content">Die Inhaltsblöcke.</param>
    /// <returns>Die referenzierten Medien.</returns>
    internal static Guid[] ImageIds(QuestionPublishRequest content) => content.Prompt.Concat(content.Explanation).Concat(content.Answers.SelectMany(answer => answer.Blocks))
        .Where(block => block.MediaId.HasValue).Select(block => block.MediaId!.Value).Distinct().Order().ToArray();

    private static async Task<IResult> Read(Guid id, LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        var question = await Own(id, db, user, ct);
        if (question == null)
        {
            return Results.NotFound();
        }

        QuestionPublishRequest content;
        try
        {
            content = await Content(question, db, ct);
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }

        var hash = await Fingerprint(content, db, ct);
        var rights = await db.QuestionRights.AsNoTracking().SingleOrDefaultAsync(item => item.QuestionId == id, ct);
        var ids = ImageIds(content);
        var media = await db.MediaAssets.AsNoTracking().Where(image => ids.Contains(image.Id) && image.OwnerAccountId == question.OwnerAccountId)
            .OrderBy(image => image.Id).Select(image => new { image.Id, image.AltText }).ToArrayAsync(ct);
        return Results.Ok(new ApiResponse<object>(new
        {
            ContentSha256 = hash,
            Stale = rights != null && rights.ContentSha256 != hash,
            Rights = rights == null ? (JsonElement?)null : JsonSerializer.Deserialize<JsonElement>(rights.PayloadJson),
            Media = media,
            content.Source,
            content.License,
        }));
    }

    private static async Task<IResult> Save(Guid id, RightsInput input, LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        if (input.Rights.ValueKind != JsonValueKind.Object || input.Rights.GetRawText().Length > 128 * 1024 || input.Rights.GetRawText().Contains("\\u0000", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("Die vollständigen Rechteangaben fehlen oder sind zu groß.");
        }

        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({id.ToString()}))", ct);
            var question = await Own(id, db, user, ct);
            if (question == null)
            {
                return Results.NotFound();
            }

            var content = await Content(question, db, ct);
            var hash = await Fingerprint(content, db, ct);
            if (hash != input.ContentSha256)
            {
                return Results.Conflict(new { Message = "Die Frage oder ein Bild wurde geändert. Bitte Inhalte und Rechte erneut prüfen." });
            }

            Validate(input.Rights, ImageIds(content));
            var rights = await db.QuestionRights.SingleOrDefaultAsync(item => item.QuestionId == id, ct);
            if (rights == null)
            {
                rights = new QuestionRights { QuestionId = id };
                db.QuestionRights.Add(rights);
            }

            rights.ContentSha256 = hash;
            rights.PayloadJson = input.Rights.GetRawText();
            rights.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Ok(new ApiResponse<object>(new { Saved = true }));
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }
    }

    private static async Task<Question?> Own(Guid id, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        AccountIdentity.TryGetAccountId(user, out var owner)
        ? await db.Questions.Include(question => question.Draft).Include(question => question.Versions)
            .SingleOrDefaultAsync(question => question.Id == id && question.OwnerAccountId == owner && question.DeletedAtUtc == null, ct)
        : null;

    private static void Validate(JsonElement rights, Guid[] images)
    {
        var names = rights.EnumerateObject().Select(property => property.Name).ToArray();
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length || names.Any(name => name is not ("license" or "provenance" or "media")))
        {
            throw new InvalidDataException("Unbekannte oder doppelte Rechtefelder.");
        }

        if (!rights.TryGetProperty("license", out var license) || !rights.TryGetProperty("provenance", out var provenance) ||
            !rights.TryGetProperty("media", out var media) || media.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("Lizenz, Herkunft und einzelne Mediennachweise sind erforderlich.");
        }

        CatalogPackageReader.License(license);
        CatalogPackageReader.Provenance(provenance);
        var found = new HashSet<Guid>();
        foreach (var asset in media.EnumerateObject())
        {
            if (!Guid.TryParse(asset.Name, out var image) || asset.Name != image.ToString() || !images.Contains(image) || !found.Add(image) || asset.Value.ValueKind != JsonValueKind.Object ||
                !asset.Value.TryGetProperty("license", out var imageLicense) || !asset.Value.TryGetProperty("provenance", out var imageProvenance) ||
                asset.Value.EnumerateObject().Any(property => property.Name is not ("license" or "provenance")))
            {
                throw new InvalidDataException("Jedes verwendete Bild benötigt genau einen eigenen Lizenz- und Herkunftsnachweis.");
            }

            CatalogPackageReader.License(imageLicense);
            CatalogPackageReader.Provenance(imageProvenance);
        }

        if (found.Count != images.Length)
        {
            throw new InvalidDataException("Einzelne Bildnachweise fehlen.");
        }
    }

    private static IResult Invalid(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["rights"] = [message] });

    /// <summary>Bindet ausdrücklich erfasste Rechte an die vorher gelesene Fassung.</summary>
    /// <param name="ContentSha256">Der geprüfte Inhaltsfingerabdruck.</param>
    /// <param name="Rights">Einzellizenzen und Herkunftsangaben.</param>
    public sealed record RightsInput(string ContentSha256, JsonElement Rights);
}
