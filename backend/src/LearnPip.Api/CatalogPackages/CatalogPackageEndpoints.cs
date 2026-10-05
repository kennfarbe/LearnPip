// <copyright file="CatalogPackageEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        group.MapPost("/preview", Preview).DisableAntiforgery().RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(CatalogPackageReader.MaxArchiveBytes + (1024 * 1024)));
        group.MapPost("/import", Import).DisableAntiforgery().RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(CatalogPackageReader.MaxArchiveBytes + (1024 * 1024)));
        group.MapGet("/", List);
        group.MapGet("/{id:guid}/original", Original);
        return app;
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
                .Select(item => new { item.Fingerprint, item.PrivateCatalogId }).SingleOrDefaultAsync(ct);
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
            return Results.Ok(new ApiResponse<CatalogPackagePreview>(preview));
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
                return existing.Fingerprint == package.Fingerprint
                    ? Results.Ok(new ApiResponse<object>(new { CatalogId = existing.PrivateCatalogId, AlreadyImported = true }))
                    : Results.Conflict(new { Message = "Das Paket wurde geändert. Bestehende Fragen und Lernstände bleiben erhalten. Kontrollierte Paketupdates sind noch nicht verfügbar." });
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
            var media = await db.MediaAssets.Where(item => item.OwnerAccountId == owner && item.DeletedAtUtc == null)
                .Select(item => item.ByteLength).ToListAsync(ct);
            var addedImages = package.Questions.Where(question => !comparison.Identical.ContainsKey(question.GetProperty("id").GetString()!)).SelectMany(question => question.GetProperty("media").EnumerateArray())
                .Select(asset => images[asset.GetProperty("path").GetString()!].LongLength).ToArray();
            if (stored.Count >= 20 || stored.Sum(size => (long)size) + package.Archive.Length > 100L * 1024 * 1024 ||
                media.Count + addedImages.Length > 100 || media.Sum() + addedImages.Sum() > 100L * 1024 * 1024)
            {
                return Results.Problem("Importkontingent erreicht: maximal 20 Originalpakete/100 MiB und 100 Bilder/100 MiB pro Konto. Kein Teilimport.", statusCode: 413);
            }

            var title = package.Manifest.GetProperty("title").GetString()!;
            var name = string.Concat(title.EnumerateRunes().Take(100));
            var suffix = 1;
            while (await db.PrivateCatalogs.AnyAsync(item => item.OwnerAccountId == owner && item.Name == name, ct))
            {
                name = string.Concat(title.EnumerateRunes().Take(100)) + " (Import " + (++suffix).ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
            }

            var catalog = new PrivateCatalog { OwnerAccountId = owner, Name = name };
            db.PrivateCatalogs.Add(catalog);
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

    private static async Task<CatalogPackage> Read(IFormFile file, CancellationToken ct)
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

    private static IResult Invalid(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["package"] = [message] });
}
