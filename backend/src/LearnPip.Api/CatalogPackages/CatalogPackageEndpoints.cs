// <copyright file="CatalogPackageEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Registriert bestätigte, vollständig private Offline-Paketimporte.</summary>
public static class CatalogPackageEndpoints
{
    private static readonly string[] NoticeFiles = ["LICENSES.md", "NOTICE", "ATTRIBUTION"];

    /// <summary>Registriert Vorschau, Bestätigung und verlustfreien Originaldownload.</summary>
    /// <param name="app">Der Routen-Builder.</param>
    /// <returns>Die ergänzten Routen für private Importpakete.</returns>
    public static IEndpointRouteBuilder MapCatalogPackageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog-packages").RequireAuthorization(ApiPolicies.ActiveAccount);
        group.MapPost("/preview", Preview).RequireQuestionPermissions("create", "import").DisableAntiforgery().RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(CatalogPackageReader.MaxArchiveBytes + (1024 * 1024)));
        group.MapPost("/import", Import).RequireQuestionPermissions("create", "import").DisableAntiforgery().RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(CatalogPackageReader.MaxArchiveBytes + (1024 * 1024)));
        group.MapGet("/", List).RequireQuestionPermissions("readOwn", "import");
        group.MapGet("/{id:guid}/original", Original).RequireQuestionPermissions("readOwn", "export");
        return app;
    }

    /// <summary>Liest einen begrenzten Upload ohne Dateiextraktion.</summary>
    /// <param name="file">Die ausgewählte lokale ZIP-Datei.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Das vollständig validierte Paket.</returns>
    internal static async Task<CatalogPackage> Read(IFormFile file, CancellationToken ct)
    {
        if (file.Length is < 1 or > CatalogPackageReader.MaxArchiveBytes)
        {
            throw new InvalidDataException("Bitte eine ZIP-Datei von maximal 25 MiB auswählen.");
        }

        await using var source = file.OpenReadStream();
        using var target = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(buffer, ct)) != 0)
        {
            if (target.Length + count > file.Length)
            {
                throw new InvalidDataException("Ungültige Uploadgröße.");
            }

            await target.WriteAsync(buffer.AsMemory(0, count), ct);
        }

        if (target.Length != file.Length)
        {
            throw new InvalidDataException("Unvollständiger Upload.");
        }

        return CatalogPackageReader.Read(target.ToArray());
    }

    private static async Task<IResult> Preview(IFormFile file, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        try
        {
            var package = await Read(file, ct);
            CatalogPackageImporter.PrepareImages(package);
            var packageId = package.Manifest.GetProperty("package_id").GetString()!;
            var existing = await db.CatalogPackageImports.AsNoTracking().Where(item => item.OwnerAccountId == owner && item.PackageId == packageId)
                .SingleOrDefaultAsync(ct);
            var state = "new";
            if (existing != null)
            {
                state = existing.Fingerprint == package.Fingerprint ? "identical" : "conflict";
            }

            var comparison = existing == null ? await CatalogPackageComparison.Compare(package, db, owner, ct) : (new Dictionary<string, Guid>(StringComparer.Ordinal), new List<string>());
            var identical = comparison.Item1.Count;
            var conflicts = comparison.Item2.Count;
            if (existing != null)
            {
                identical = state == "identical" ? package.Questions.Count : 0;
                conflicts = state == "conflict" ? package.Questions.Count : 0;
            }

            if (existing == null)
            {
                if (conflicts > 0)
                {
                    state = "conflict";
                }
                else if (identical == package.Questions.Count)
                {
                    state = "identical";
                }
            }

            var canUpdate = false;
            if (existing != null && existing.Fingerprint != package.Fingerprint)
            {
                try
                {
                    await CatalogPackageUpdates.Candidates(existing, db, ct);
                    canUpdate = existing.PrivateCatalogId.HasValue;
                }
                catch (InvalidDataException)
                {
                    canUpdate = false;
                }
            }

            var licenses = package.Questions.SelectMany(question => question.GetProperty("media").EnumerateArray()
                .Select(asset => asset.GetProperty("license")).Prepend(question.GetProperty("license"))).DistinctBy(license => license.GetRawText()).ToArray();
            var notices = NoticeFiles.ToDictionary(name => name, name => Encoding.UTF8.GetString(package.Files[name]), StringComparer.Ordinal);
            var preview = new CatalogPackagePreview(
                packageId,
                package.Manifest.GetProperty("title").GetString()!,
                package.Manifest.GetProperty("catalog_version").GetString()!,
                package.Manifest.GetProperty("schema_version").GetString()!,
                package.Manifest.GetProperty("source_revision").GetString()!,
                package.Manifest.GetProperty("language").GetString()!,
                package.Questions.Count,
                package.Files.Keys.Count(path => path.StartsWith("media/", StringComparison.Ordinal)),
                package.Archive.LongLength,
                package.Files.Values.Sum(bytes => bytes.LongLength),
                package.Manifest.GetProperty("license"),
                licenses,
                notices,
                package.Questions.SelectMany(question => question.GetProperty("topics").EnumerateArray().Select(topic => topic.GetString()!))
                    .Distinct(StringComparer.Ordinal).ToArray(),
                Convert.ToHexStringLower(SHA256.HashData(package.Archive)),
                state,
                existing?.PrivateCatalogId,
                package.Questions.Count - identical - conflicts,
                identical,
                conflicts);
            var original = existing == null ? null : CatalogPackageReader.Read(existing.Archive);
            return Results.Ok(new ApiResponse<CatalogPackagePreview>(preview with
            {
                CanUpdate = canUpdate,
                PreviousFingerprint = existing?.Fingerprint,
                PreviousCatalogVersion = existing?.CatalogVersion,
                PreviousSourceRevision = original?.Manifest.GetProperty("source_revision").GetString(),
                Changes = CatalogPackageChanges.Compare(original, package),
            }));
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }
    }

    private static async Task<IResult> Import(
        IFormFile file,
        [FromForm] string archiveSha256,
        [FromForm] bool rightsConfirmed,
        [FromForm] Guid? targetCatalogId,
        [FromForm] bool? updateConfirmed,
        [FromForm] string? previousFingerprint,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        if (!rightsConfirmed)
        {
            return Invalid("Bitte die Nutzungsrechte und den privaten Import ausdrücklich bestätigen.");
        }

        try
        {
            var package = await Read(file, ct);
            if (!string.Equals(archiveSha256, Convert.ToHexStringLower(SHA256.HashData(package.Archive)), StringComparison.Ordinal))
            {
                return Invalid("Die Datei stimmt nicht mit der bestätigten Vorschau überein. Bitte erneut prüfen.");
            }

            var images = CatalogPackageImporter.PrepareImages(package);
            var packageId = package.Manifest.GetProperty("package_id").GetString()!;
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({owner.ToString()}))", ct);
            var existing = await db.CatalogPackageImports.SingleOrDefaultAsync(item => item.OwnerAccountId == owner && item.PackageId == packageId, ct);
            if (existing != null)
            {
                if (existing.Fingerprint == package.Fingerprint)
                {
                    return Results.Ok(new ApiResponse<object>(new { CatalogId = existing.PrivateCatalogId, AlreadyImported = true }));
                }

                if (updateConfirmed != true || previousFingerprint != existing.Fingerprint)
                {
                    return Results.Conflict(new { Message = "Das Paket wurde geändert. Bitte den aktuellen Paketstand und die Updateauswirkungen ausdrücklich bestätigen." });
                }

                var updates = await CatalogPackageUpdates.Candidates(existing, db, ct);
                var updateCatalog = existing.PrivateCatalogId.HasValue
                    ? await db.PrivateCatalogs.SingleOrDefaultAsync(item => item.Id == existing.PrivateCatalogId && item.OwnerAccountId == owner, ct)
                    : null;
                if (updateCatalog == null)
                {
                    return Invalid("Der ursprüngliche private Zielkatalog fehlt. Bitte Konflikt zuerst lösen.");
                }

                var storedArchives = await db.CatalogPackageImports.Where(item => item.OwnerAccountId == owner).SumAsync(item => (long)item.Archive.Length, ct);
                var storedHistory = await db.CatalogPackageImportRevisions.Where(item => item.OwnerAccountId == owner).SumAsync(item => (long)item.Archive.Length, ct);
                var imageSizes = await db.MediaAssets.Where(item => item.OwnerAccountId == owner && item.DeletedAtUtc == null).Select(item => item.ByteLength).ToListAsync(ct);
                var incoming = package.Questions.SelectMany(question => question.GetProperty("media").EnumerateArray()).Select(asset => images[asset.GetProperty("path").GetString()!].LongLength).ToArray();
                if (storedArchives + storedHistory + package.Archive.Length > 100L * 1024 * 1024 || !PrivateImageQuota.Fits(imageSizes.Count + incoming.Length, imageSizes.Sum() + incoming.Sum()))
                {
                    return Results.Problem("Updatekontingent erreicht. Historische Originalpakete und Bilder werden für den Erhalt von Nachweisen und Lernständen mitgerechnet.", statusCode: 413);
                }

                var updateComparison = await CatalogPackageComparison.Compare(package, db, owner, ct);
                if (updateComparison.Conflicts.Any(sourceId => !updates.ContainsKey(sourceId)))
                {
                    return Results.Conflict(new { Message = "Andere importierte Quellfragen stehen im Konflikt. Kein Teilupdate." });
                }

                var identical = updateComparison.Identical.Where(pair => !updates.ContainsKey(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
                db.CatalogPackageImportRevisions.Add(new CatalogPackageImportRevision
                {
                    OwnerAccountId = owner,
                    PackageId = existing.PackageId,
                    Archive = existing.Archive,
                    QuestionIdsJson = existing.QuestionIdsJson,
                    QuestionVersionIdsJson = existing.QuestionVersionIdsJson,
                });
                var updateIds = CatalogPackageImporter.AddQuestions(db, package, images, owner, updateCatalog, identical, updates);
                existing.Archive = package.Archive;
                existing.CatalogVersion = package.Manifest.GetProperty("catalog_version").GetString()!;
                existing.Fingerprint = package.Fingerprint;
                existing.QuestionIdsJson = JsonSerializer.Serialize(updateIds);
                existing.QuestionVersionIdsJson = await CatalogPackageUpdates.Versions(db, updateIds, ct);
                existing.ImportedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return Results.Ok(new ApiResponse<object>(new { CatalogId = updateCatalog.Id, AlreadyImported = false, Updated = true }));
            }

            var comparison = await CatalogPackageComparison.Compare(package, db, owner, ct);
            if (comparison.Conflicts.Count > 0)
            {
                return Results.Conflict(new { Message = "Bekannte Quellfragen wurden geändert oder bereits bearbeitet. Kein Überschreiben und keine Duplikate; bitte Auswahl oder Quell-IDs prüfen." });
            }

            if (comparison.Identical.Count == package.Questions.Count)
            {
                return Results.Ok(new ApiResponse<object>(new { CatalogId = (Guid?)null, AlreadyImported = true }));
            }

            var stored = await db.CatalogPackageImports.Where(item => item.OwnerAccountId == owner)
                .Select(item => item.Archive.Length).ToListAsync(ct);
            var historyBytes = await db.CatalogPackageImportRevisions.Where(item => item.OwnerAccountId == owner).SumAsync(item => (long)item.Archive.Length, ct);
            var media = await db.MediaAssets.Where(item => item.OwnerAccountId == owner && item.DeletedAtUtc == null)
                .Select(item => item.ByteLength).ToListAsync(ct);
            var addedImages = package.Questions.Where(question => !comparison.Identical.ContainsKey(question.GetProperty("id").GetString()!)).SelectMany(question => question.GetProperty("media").EnumerateArray())
                .Select(asset => images[asset.GetProperty("path").GetString()!].LongLength).ToArray();
            if (stored.Count >= 20 || stored.Sum(size => (long)size) + historyBytes + package.Archive.Length > 100L * 1024 * 1024 ||
                !PrivateImageQuota.Fits(media.Count + addedImages.Length, media.Sum() + addedImages.Sum()))
            {
                return Results.Problem("Importkontingent erreicht: maximal 20 Originalpakete/100 MiB und 2000 Bilder/100 MiB pro Konto. Kein Teilimport.", statusCode: 413);
            }

            PrivateCatalog? destination = null;
            if (targetCatalogId.HasValue)
            {
                destination = await db.PrivateCatalogs.SingleOrDefaultAsync(item => item.Id == targetCatalogId && item.OwnerAccountId == owner, ct);
                if (destination == null)
                {
                    return Results.NotFound();
                }
            }

            var title = package.Manifest.GetProperty("title").GetString()!;
            var name = string.Concat(title.EnumerateRunes().Take(100));
            var suffix = 1;
            while (await db.PrivateCatalogs.AnyAsync(item => item.OwnerAccountId == owner && item.Name == name, ct))
            {
                name = string.Concat(title.EnumerateRunes().Take(100)) + " (Import " + (++suffix).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            }

            var catalog = destination ?? new PrivateCatalog { OwnerAccountId = owner, Name = name };
            if (destination == null)
            {
                db.PrivateCatalogs.Add(catalog);
            }

            var ids = CatalogPackageImporter.AddQuestions(db, package, images, owner, catalog, comparison.Identical);
            db.CatalogPackageImports.Add(new CatalogPackageImport
            {
                OwnerAccountId = owner,
                PrivateCatalogId = catalog.Id,
                PackageId = packageId,
                CatalogVersion = package.Manifest.GetProperty("catalog_version").GetString()!,
                Fingerprint = package.Fingerprint,
                Archive = package.Archive,
                QuestionIdsJson = JsonSerializer.Serialize(ids),
                QuestionVersionIdsJson = await CatalogPackageUpdates.Versions(db, ids, ct),
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Created($"/api/v1/catalogs/{catalog.Id}", new ApiResponse<object>(new { CatalogId = catalog.Id, AlreadyImported = false }));
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }
    }

    private static async Task<IResult> List(LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        var items = await db.CatalogPackageImports.AsNoTracking().Where(item => item.OwnerAccountId == owner)
            .OrderByDescending(item => item.ImportedAtUtc)
            .Select(item => new { item.Id, item.PackageId, item.CatalogVersion, item.PrivateCatalogId, item.ImportedAtUtc }).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> Original(Guid id, LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        var archive = await db.CatalogPackageImports.AsNoTracking().Where(item => item.Id == id && item.OwnerAccountId == owner)
            .Select(item => item.Archive).SingleOrDefaultAsync(ct);
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return archive == null ? Results.NotFound() : Results.File(archive, "application/zip", $"learnpip-{id:N}.zip");
    }

    private static IResult Invalid(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["package"] = [message] });
}
