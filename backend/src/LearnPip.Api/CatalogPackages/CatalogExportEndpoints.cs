// <copyright file="CatalogExportEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Exportiert ausschließlich eigene Fragen nach Vorschau und bestätigten Rechten.</summary>
public static class CatalogExportEndpoints
{
    private static readonly string[] NoticeNames = ["LICENSES.md", "NOTICE", "ATTRIBUTION"];

    /// <summary>Registriert Auswahl, Exportvorschau und privaten ZIP-Download.</summary>
    /// <param name="app">Der Routen-Builder.</param>
    /// <returns>Die ergänzten Routen.</returns>
    public static IEndpointRouteBuilder MapCatalogExportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog-exports").RequireAuthorization(ApiPolicies.ActiveAccount);
        group.MapGet("/questions", Candidates);
        group.MapPost("/preview", Preview).RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(1024 * 1024));
        group.MapPost("/download", Download).RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(1024 * 1024));
        return app;
    }

    private static async Task<IResult> Candidates(LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        Private(context);
        var questions = await db.Questions.AsNoTracking().Where(question => question.OwnerAccountId == owner && question.DeletedAtUtc == null)
            .Include(question => question.Draft).Include(question => question.Versions).OrderBy(question => question.CreatedAtUtc).Take(10001).ToListAsync(ct);
        if (questions.Count > 10000)
        {
            return Results.Problem("Die Auswahlübersicht ist auf 10000 Fragen begrenzt.", statusCode: 413);
        }

        var items = questions.Select(question =>
        {
            var version = question.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();
            var draft = question.Draft == null ? null : JsonSerializer.Deserialize<QuestionPublishRequest>(question.Draft.PayloadJson);
            return new
            {
                question.Id,
                CatalogId = question.PrivateCatalogId,
                Prompt = draft == null ? version?.Prompt ?? "Unvollständige Frage" : Summary(draft.Prompt, true),
                Subject = draft?.Subject ?? version?.Subject ?? string.Empty,
                Topic = draft?.Topic ?? version?.Topic ?? string.Empty,
                Language = draft?.Language ?? version?.Language ?? string.Empty,
                HasDraft = draft != null,
            };
        }).ToArray();
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> Preview(CatalogExportRequest input, LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, IConfiguration configuration, CancellationToken ct)
    {
        Private(context);
        return await Process(input, db, user, false, configuration, ct);
    }

    private static async Task<IResult> Download(CatalogExportRequest input, LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, IConfiguration configuration, CancellationToken ct)
    {
        Private(context);
        return await Process(input, db, user, true, configuration, ct);
    }

    private static async Task<IResult> Process(CatalogExportRequest input, LearnPipDbContext db, ClaimsPrincipal user, bool download, IConfiguration configuration, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        if (input.Purpose is not ("private" or "community"))
        {
            return Invalid("Unbekannter Exportzweck.");
        }

        if (input.QuestionIds is not { Count: >= 1 and <= 10000 } || input.QuestionIds.Distinct().Count() != input.QuestionIds.Count ||
            string.IsNullOrWhiteSpace(input.Title) || input.Title.Length > 256 || string.IsNullOrWhiteSpace(input.Publisher) || input.Publisher.Length > 256 ||
            input.LicenseNotice == null || input.LicenseNotice.Length > 32000 ||
            input.QuestionLicense.ValueKind != JsonValueKind.Object || input.ImageLicense.ValueKind != JsonValueKind.Object)
        {
            return Invalid("Bitte 1 bis 10000 unterschiedliche eigene Fragen, einen Titel und einen Attributionsnamen auswählen.");
        }

        if (download && !input.RightsConfirmed)
        {
            return Invalid("Bitte Originalrechte an eigenen Texten/Bildern und die Exportrechte ausdrücklich bestätigen.");
        }

        try
        {
            // One read snapshot prevents concurrent saves/deletions from mixing incompatible contents and media.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            var selected = await db.Questions.AsNoTracking().Where(question => input.QuestionIds.Contains(question.Id) && question.OwnerAccountId == owner && question.DeletedAtUtc == null)
                .Include(question => question.Draft).Include(question => question.Versions).ToListAsync(ct);
            if (selected.Count != input.QuestionIds.Count)
            {
                return Results.NotFound();
            }

            var mappings = await db.CatalogPackageImports.AsNoTracking().Where(item => item.OwnerAccountId == owner)
                .OrderByDescending(item => item.ImportedAtUtc).Select(item => new { item.Id, item.QuestionIdsJson, item.QuestionVersionIdsJson }).ToListAsync(ct);
            var history = await db.CatalogPackageImportRevisions.AsNoTracking().Where(item => item.OwnerAccountId == owner)
                .OrderByDescending(item => item.ArchivedAtUtc).Select(item => new { item.Id, item.QuestionIdsJson, item.QuestionVersionIdsJson }).ToListAsync(ct);
            var origins = new Dictionary<Guid, (Guid ImportId, string SourceId, string VersionsJson)>();
            foreach (var mapping in mappings.Concat(history))
            {
                foreach (var (sourceId, localId) in JsonSerializer.Deserialize<Dictionary<string, Guid>>(mapping.QuestionIdsJson)!)
                {
                    origins.TryAdd(localId, (mapping.Id, sourceId, mapping.QuestionVersionIdsJson));
                }
            }

            var originals = new Dictionary<Guid, CatalogPackage>();
            var media = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var mediaTypes = new Dictionary<string, string>(StringComparer.Ordinal);
            var questions = new List<JsonObject>();
            var notices = NoticeNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);
            notices["LICENSES.md"].Add("# Lizenznachweise\n\nKeine Softwarelizenz für Frageinhalte. Einzellizenzen sind maßgeblich.\n\n" + input.LicenseNotice);
            notices["NOTICE"].Add("Lokaler Inhaltsdownload. Keine Veröffentlichung, keine Übertragung von Konten oder Lernständen. LicenseRef-Private bedeutet ausschließlich private Nutzung durch berechtigte Empfänger; keine offene Lizenz und keine Community-Freigabe. LicenseRef-Collection ersetzt keine Einzellizenz.");
            notices["ATTRIBUTION"].Add("Ausdrücklich angegebener Herausgeber: " + input.Publisher);
            foreach (var question in selected.OrderBy(item => item.Id))
            {
                ct.ThrowIfCancellationRequested();
                if (origins.TryGetValue(question.Id, out var origin))
                {
                    if (!originals.TryGetValue(origin.ImportId, out var package))
                    {
                        var bytes = await db.CatalogPackageImports.Where(item => item.Id == origin.ImportId && item.OwnerAccountId == owner).Select(item => item.Archive).SingleOrDefaultAsync(ct)
                            ?? await db.CatalogPackageImportRevisions.Where(item => item.Id == origin.ImportId && item.OwnerAccountId == owner).Select(item => item.Archive).SingleAsync(ct);
                        package = CatalogPackageReader.Read(bytes);
                        originals.Add(origin.ImportId, package);
                        foreach (var name in NoticeNames)
                        {
                            notices[name].Add(System.Text.Encoding.UTF8.GetString(package.Files[name]));
                        }
                    }

                    if (CatalogPackageUpdates.Unedited(question, origin.VersionsJson, origin.SourceId))
                    {
                        questions.Add(Original(package, origin.SourceId, media, mediaTypes));
                    }
                    else
                    {
                        if (!await db.QuestionRights.AnyAsync(item => item.QuestionId == question.Id, ct))
                        {
                            throw new InvalidDataException("Bearbeitete Importfragen benötigen vollständige Einzelnachweise. Bitte Quellen und Rechte je Frage und Bild erfassen.");
                        }

                        var adapted = await Native(question, input, db, owner, media, mediaTypes, ct);
                        var original = package.Questions.Single(item => item.GetProperty("id").GetString() == origin.SourceId);
                        if (!await db.QuestionRights.AnyAsync(item => item.QuestionId == question.Id, ct) ||
                            adapted["provenance"]!["kind"]!.GetValue<string>() != "adapted" ||
                            adapted["license"]!["id"]!.GetValue<string>() != original.GetProperty("license").GetProperty("id").GetString())
                        {
                            throw new InvalidDataException("Bearbeitete Importfragen benötigen ausdrückliche Bearbeitungsnachweise und die unveränderte Quelllizenz. Keine automatische Umlizenzierung.");
                        }

                        adapted["id"] = origin.SourceId;
                        var manifest = JsonNode.Parse(package.Manifest.GetRawText())!.AsObject();
                        manifest.Remove("files");
                        adapted["origin"] = manifest;
                        if (original.TryGetProperty("age_band", out var ageBand))
                        {
                            adapted["age_band"] = ageBand.GetString();
                        }

                        adapted["difficulty"] = original.GetProperty("difficulty").GetString();
                        var topics = original.GetProperty("topics").EnumerateArray().Select(topic => topic.GetString()!)
                            .Concat(new[] { adapted["subject"]!.GetValue<string>(), adapted["topic"]!.GetValue<string>() }).Distinct(StringComparer.Ordinal);
                        adapted["topics"] = new JsonArray(topics.Select(topic => (JsonNode)JsonValue.Create(topic)).ToArray());
                        notices["ATTRIBUTION"].Add("Originalnachweis der bearbeiteten Frage: " + original.GetRawText());
                        questions.Add(adapted);
                    }
                }
                else
                {
                    questions.Add(await Native(question, input, db, owner, media, mediaTypes, ct));
                }
            }

            if (questions.Select(question => question["id"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count() != questions.Count)
            {
                throw new InvalidDataException("Die Auswahl enthält dieselbe Quellfrage mehrfach. Bitte eine eindeutige Teilmenge auswählen.");
            }

            foreach (var question in questions)
            {
                notices["LICENSES.md"].Add(question["id"] + "\n" + question["license"]!.ToJsonString() + "\n" + question["media"]!.ToJsonString());
                notices["ATTRIBUTION"].Add(question["id"] + "\n" + question["license"]!.ToJsonString() + "\n" + question["provenance"]!.ToJsonString());
            }

            var exported = CatalogPackageWriter.Write(
                input.Title,
                input.Publisher,
                questions,
                media,
                notices.ToDictionary(item => item.Key, item => string.Join("\n\n", item.Value.Distinct(StringComparer.Ordinal)), StringComparer.Ordinal),
                selected.Max(question => question.Draft?.UpdatedAtUtc ?? question.UpdatedAtUtc),
                mediaTypes);
            var report = CatalogRightsReport.Inspect(exported.Questions);
            var communityEnabled = configuration.GetValue<bool>("CatalogPackages:CommunityExportEnabled");
            var hash = Convert.ToHexStringLower(SHA256.HashData(exported.Archive));
            if (download)
            {
                if (input.Purpose == "community" && (!communityEnabled || report.Count != 0 || !input.PublicationConfirmed))
                {
                    return Invalid("Community-Export gesperrt: Administratorfreigabe, vollständige offene Einzelnachweise und ausdrückliche Bestätigung der unwiderruflichen offenen Weitergabe sind erforderlich. Privat exportieren oder Nachweise berichtigen.");
                }

                if (input.PreviewSha256 != hash)
                {
                    return Results.Conflict(new { Message = "Auswahl, Inhalt oder Rechteangaben haben sich seit der Vorschau geändert. Bitte erneut prüfen." });
                }

                return Results.File(exported.Archive, "application/zip", "learnpip-selection.zip");
            }

            return Results.Ok(new ApiResponse<object>(new
            {
                RightsReport = report,
                CommunityEnabled = communityEnabled,
                CommunityEligible = communityEnabled && report.Count == 0,
                QuestionCount = exported.Questions.Count,
                MediaCount = media.Count,
                ArchiveBytes = exported.Archive.Length,
                PreviewSha256 = hash,
                SchemaVersion = "0.2.0",
                Topics = exported.Questions.SelectMany(question => question.GetProperty("topics").EnumerateArray().Select(topic => topic.GetString())).Distinct(StringComparer.Ordinal).ToArray(),
                Licenses = exported.Questions.SelectMany(question => question.GetProperty("media").EnumerateArray().Select(asset => asset.GetProperty("license")).Prepend(question.GetProperty("license")))
                    .DistinctBy(license => license.GetRawText()).ToArray(),
                Notices = NoticeNames.ToDictionary(name => name, name => System.Text.Encoding.UTF8.GetString(exported.Files[name]), StringComparer.Ordinal),
            }));
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }
    }

    private static async Task<JsonObject> Native(Question question, CatalogExportRequest input, LearnPipDbContext db, Guid owner, Dictionary<string, byte[]> media, Dictionary<string, string> mediaTypes, CancellationToken ct)
    {
        var content = await CatalogRightsEndpoints.Content(question, db, ct);
        var storedRights = await db.QuestionRights.AsNoTracking().SingleOrDefaultAsync(item => item.QuestionId == question.Id, ct);
        JsonElement? rights = storedRights == null ? null : JsonSerializer.Deserialize<JsonElement>(storedRights.PayloadJson);
        if (storedRights != null && storedRights.ContentSha256 != await CatalogRightsEndpoints.Fingerprint(content, db, ct))
        {
            throw new InvalidDataException("Die Frage oder ein Bild wurde geändert. Bitte die Einzelnachweise erneut prüfen und speichern.");
        }

        if (QuestionEndpoints.Validate(content with { Source = string.IsNullOrWhiteSpace(content.Source) ? "Eigenes Original" : content.Source, License = string.IsNullOrWhiteSpace(content.License) ? "LicenseRef-Private" : content.License }) is { } error)
        {
            throw new InvalidDataException("Unvollständige Frage: " + error);
        }

        var textLicense = rights?.GetProperty("license") ?? input.QuestionLicense;
        if ((rights == null && Uri.TryCreate(content.Source, UriKind.Absolute, out _)) || (!string.IsNullOrWhiteSpace(content.License) &&
            (!textLicense.TryGetProperty("id", out var licenseId) || licenseId.ValueKind != JsonValueKind.String || licenseId.GetString() != content.License)))
        {
            throw new InvalidDataException("Diese Frage enthält Drittquellen oder eine abweichende Lizenz. Keine automatische Umetikettierung: vollständige Einzelnachweise müssen zuerst erfasst werden.");
        }

        var imageIds = content.Prompt.Concat(content.Explanation).Concat(content.Answers.SelectMany(answer => answer.Blocks))
            .Where(block => block.MediaId.HasValue).Select(block => block.MediaId!.Value).Distinct().ToArray();
        var images = await db.MediaAssets.AsNoTracking().Where(image => imageIds.Contains(image.Id) && image.OwnerAccountId == owner && image.DeletedAtUtc == null &&
            (image.QuestionVersionId == null || image.QuestionVersion!.Question.DeletedAtUtc == null)).ToListAsync(ct);
        if (images.Count != imageIds.Length)
        {
            throw new InvalidDataException("Ein Bild fehlt oder gehört nicht zum eigenen Konto. Kein Teilexport.");
        }

        var paths = new Dictionary<Guid, string>();
        var assets = new JsonArray();
        foreach (var image in images.OrderBy(image => image.Id))
        {
            if (image.MediaType is not ("image/jpeg" or "image/png") || string.IsNullOrWhiteSpace(image.AltText))
            {
                throw new InvalidDataException("Ein Bild benötigt einen Alternativtext und ein unterstütztes JPEG-/PNG-Format.");
            }

            var bytes = await db.MediaBlobs.Where(blob => blob.MediaAssetId == image.Id).Select(blob => blob.Data).SingleOrDefaultAsync(ct)
                ?? throw new InvalidDataException("Die Bilddatei fehlt. Kein Teilexport.");
            var path = MediaPath(bytes, "-" + image.Id.ToString("N") + (image.MediaType == "image/png" ? ".png" : ".jpg"));
            media.TryAdd(path, bytes);
            mediaTypes[path] = image.MediaType;
            paths.Add(image.Id, path);
            if (assets.Any(asset => asset!["path"]!.GetValue<string>() == path))
            {
                throw new InvalidDataException("Identische Bilddateien mit verschiedenen Bildkennungen benötigen eine eindeutige Medienbeschreibung.");
            }

            assets.Add(new JsonObject
            {
                ["path"] = path,
                ["alt"] = image.AltText,
                ["license"] = JsonNode.Parse((rights?.GetProperty("media").GetProperty(image.Id.ToString()).GetProperty("license") ?? input.ImageLicense).GetRawText()),
                ["provenance"] = rights == null ? new JsonObject { ["kind"] = "original" } : JsonNode.Parse(rights.Value.GetProperty("media").GetProperty(image.Id.ToString()).GetProperty("provenance").GetRawText()),
            });
        }

        JsonArray Blocks(IReadOnlyList<ContentBlockInput> blocks) => new(blocks.Select(block => (JsonNode)(block.Kind == "text"
            ? new JsonObject { ["kind"] = "text", ["text"] = block.Text }
            : new JsonObject { ["kind"] = "image", ["path"] = paths[block.MediaId!.Value] })).ToArray());
        var answers = new JsonArray();
        var correct = new JsonArray();
        foreach (var (answer, index) in content.Answers.Select((answer, index) => (answer, index)))
        {
            var id = "a" + index.ToString(CultureInfo.InvariantCulture);
            answers.Add(new JsonObject { ["id"] = id, ["text"] = Summary(answer.Blocks, true), ["blocks"] = Blocks(answer.Blocks) });
            if (answer.IsCorrect)
            {
                correct.Add(id);
            }
        }

        return new JsonObject
        {
            ["id"] = "learnpip-question:" + question.Id.ToString("N"),
            ["language"] = content.Language,
            ["source_note"] = content.Source ?? string.Empty,
            ["prompt"] = Summary(content.Prompt, true),
            ["explanation"] = Summary(content.Explanation, false),
            ["prompt_blocks"] = Blocks(content.Prompt),
            ["explanation_blocks"] = Blocks(content.Explanation),
            ["answers"] = answers,
            ["correct_answer_ids"] = correct,
            ["subject"] = content.Subject,
            ["topic"] = content.Topic,
            ["topics"] = new JsonArray(new[] { content.Subject, content.Topic }.Distinct(StringComparer.Ordinal).Select(value => (JsonNode)JsonValue.Create(value)).ToArray()),
            ["difficulty"] = "unknown",
            ["selection_mode"] = content.SelectionMode,
            ["question_version"] = (question.Draft?.UpdatedAtUtc ?? question.UpdatedAtUtc).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ["license"] = JsonNode.Parse(textLicense.GetRawText()),
            ["provenance"] = rights == null ? new JsonObject { ["kind"] = "original" } : JsonNode.Parse(rights.Value.GetProperty("provenance").GetRawText()),
            ["media"] = assets,
        };
    }

    private static JsonObject Original(CatalogPackage package, string id, Dictionary<string, byte[]> media, Dictionary<string, string> mediaTypes)
    {
        var original = package.Questions.Single(question => question.GetProperty("id").GetString() == id);
        var result = CatalogPackageComparison.Promote(original, package.Manifest);

        var rename = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var asset in result["media"]!.AsArray())
        {
            var path = asset!["path"]!.GetValue<string>();
            var bytes = package.Files[path];
            var record = package.Manifest.GetProperty("files").EnumerateArray().Single(file => file.GetProperty("path").GetString() == path);
            var hasType = record.TryGetProperty("media_type", out var type);
            var salt = path + (hasType ? "\n" + type.GetString() : string.Empty);
            var target = MediaPath(bytes, "-" + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(salt)))[..16] + Path.GetExtension(path).ToLowerInvariant());
            media.TryAdd(target, bytes);
            if (hasType)
            {
                mediaTypes[target] = type.GetString()!;
            }

            rename.Add(path, target);
            asset["path"] = target;
        }

        foreach (var block in result["prompt_blocks"]!.AsArray().Concat(result["explanation_blocks"]!.AsArray())
            .Concat(result["answers"]!.AsArray().SelectMany(answer => answer!["blocks"]!.AsArray())).Where(block => block!["kind"]!.GetValue<string>() == "image"))
        {
            block!["path"] = rename[block["path"]!.GetValue<string>()];
        }

        return result;
    }

    private static string Summary(IReadOnlyList<ContentBlockInput>? blocks, bool required)
    {
        var text = blocks?.Where(block => block != null && block.Kind == "text").Select(block => block.Text).ToArray() ?? [];
        return text.Length == 0 && required ? "[Bild]" : string.Join("\n", text);
    }

    private static string MediaPath(byte[] bytes, string extension) => "media/" + Convert.ToHexStringLower(SHA256.HashData(bytes)) + extension;

    private static void Private(HttpContext context)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
    }

    private static IResult Invalid(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["export"] = [message] });
}
