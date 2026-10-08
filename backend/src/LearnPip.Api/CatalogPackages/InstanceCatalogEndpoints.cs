// <copyright file="InstanceCatalogEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Verwaltet optionale unveränderliche Instanzpakete; Lernstände bleiben unabhängig.</summary>
public static class InstanceCatalogEndpoints
{
    private static readonly string[] NoticeFiles = ["LICENSES.md", "NOTICE", "ATTRIBUTION"];

    /// <summary>Registriert ausschließlich bestätigte Paketverwaltung und verfügbare Downloads.</summary>
    /// <param name="app">Der Routen-Builder.</param>
    /// <returns>Die ergänzten Routen.</returns>
    public static IEndpointRouteBuilder MapInstanceCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/instance-catalogs").RequireAuthorization(ApiPolicies.ActiveAccount);
        group.MapGet("/", ListAvailable);
        group.MapGet("/{id:guid}/archive", Archive);
        var admin = group.MapGroup("/admin").RequireAuthorization(ApiPolicies.Admin);
        admin.MapGet("/", All);
        admin.MapPost("/preview", Preview).DisableAntiforgery().RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(CatalogPackageReader.MaxArchiveBytes + (1024 * 1024)));
        admin.MapPost("/install", Install).DisableAntiforgery().RequireRateLimiting("content-write")
            .WithMetadata(new RequestSizeLimitAttribute(CatalogPackageReader.MaxArchiveBytes + (1024 * 1024)));
        admin.MapPut("/{id:guid}/availability", Availability).RequireRateLimiting("content-write");
        admin.MapDelete("/{id:guid}", Remove).RequireRateLimiting("content-write");
        return app;
    }

    /// <summary>Speichert ausschließlich eine vollständig geprüfte und bewusst freigegebene Fassung.</summary>
    /// <param name="package">Das vollständig validierte Paket.</param>
    /// <param name="previousSha256">Der ausdrücklich bestätigte bisherige Stand.</param>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="user">Der handelnde Administrator oder lokale Installationsprozess.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Erfolg oder ein Konflikt ohne Teiländerung.</returns>
    internal static async Task<IResult> Store(CatalogPackage package, string? previousSha256, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var hash = Hash(package.Archive);
        var id = package.Manifest.GetProperty("package_id").GetString()!;
        var version = package.Manifest.GetProperty("catalog_version").GetString()!;
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"instance-catalog:" + id}))", ct);
        var ledgerKey = Hash(Encoding.UTF8.GetBytes(id + "\n" + version));
        if (await db.AdministrationAuditEvents.AnyAsync(item => item.Action == "catalog-package-delete" && item.Target == ledgerKey, ct))
        {
            return Results.Conflict(new { Message = "Diese Paketfassung wurde entfernt. Für eine erneute Bereitstellung ist eine neue Inhaltsversion erforderlich." });
        }

        var existing = await db.InstanceCatalogPackages.SingleOrDefaultAsync(item => item.PackageId == id && item.CatalogVersion == version, ct);
        if (existing != null)
        {
            return existing.ArchiveSha256 == hash ? Results.Ok(new ApiResponse<object>(new { existing.Id, AlreadyInstalled = true }))
                : Results.Conflict(new { Message = "Eine veröffentlichte Paketfassung darf nicht unter derselben Versionsnummer verändert werden." });
        }

        var previous = await db.InstanceCatalogPackages.SingleOrDefaultAsync(item => item.PackageId == id && item.Available, ct);
        if (previous?.ArchiveSha256 != previousSha256)
        {
            return Results.Conflict(new { Message = "Der Paketstand hat sich seit der Vorschau geändert. Bitte erneut prüfen." });
        }

        var sizes = await db.InstanceCatalogPackages.Select(item => item.Archive.Length).ToListAsync(ct);
        if (sizes.Count >= 100 || sizes.Sum(size => (long)size) + package.Archive.Length > 500L * 1024 * 1024)
        {
            return Results.Problem("Instanzkontingent erreicht: 100 Paketfassungen oder 500 MiB. Kein Teilimport.", statusCode: 413);
        }

        if (previous != null)
        {
            previous.Available = false;
        }

        var record = new InstanceCatalogPackage { PackageId = id, CatalogVersion = version, Archive = package.Archive, ArchiveSha256 = hash, Available = true };
        db.InstanceCatalogPackages.Add(record);
        Audit(db, user, "catalog-package-install", record.Id, previous?.ArchiveSha256, hash);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.Created($"/api/v1/instance-catalogs/{record.Id}", new ApiResponse<object>(new { record.Id, AlreadyInstalled = false }));
    }

    private static async Task<IResult> ListAvailable(LearnPipDbContext db, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        var records = await db.InstanceCatalogPackages.AsNoTracking().Where(package => package.Available).OrderBy(package => package.PackageId).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(records.Select(Metadata).ToArray()));
    }

    private static async Task<IResult> All(LearnPipDbContext db, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        var records = await db.InstanceCatalogPackages.AsNoTracking().OrderByDescending(package => package.InstalledAtUtc).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(records.Select(Metadata).ToArray()));
    }

    private static async Task<IResult> Archive(Guid id, LearnPipDbContext db, HttpContext context, CancellationToken ct)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        var bytes = await db.InstanceCatalogPackages.AsNoTracking().Where(package => package.Id == id && package.Available)
            .Select(package => package.Archive).SingleOrDefaultAsync(ct);
        return bytes == null ? Results.NotFound() : Results.File(bytes, "application/zip", "learnpip-package.zip");
    }

    private static async Task<IResult> Preview(IFormFile file, LearnPipDbContext db, CancellationToken ct)
    {
        try
        {
            var package = await CatalogPackageEndpoints.Read(file, ct);
            CatalogPackageImporter.PrepareImages(package);
            var packageId = package.Manifest.GetProperty("package_id").GetString()!;
            var previous = await db.InstanceCatalogPackages.AsNoTracking().Where(item => item.PackageId == packageId && item.Available).SingleOrDefaultAsync(ct);
            var original = previous == null ? null : CatalogPackageReader.Read(previous.Archive);
            var before = original?.Questions.Select(question => question.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal) ?? [];
            var after = package.Questions.Select(question => question.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);
            var record = new InstanceCatalogPackage { PackageId = packageId, CatalogVersion = package.Manifest.GetProperty("catalog_version").GetString()!, Archive = package.Archive, ArchiveSha256 = Hash(package.Archive) };
            return Results.Ok(new ApiResponse<object>(new
            {
                Package = Metadata(record),
                PreviousId = previous?.Id,
                PreviousVersion = previous?.CatalogVersion,
                PreviousSha256 = previous?.ArchiveSha256,
                NewQuestions = after.Except(before).Count(),
                RemovedQuestions = before.Except(after).Count(),
                SharedQuestions = after.Intersect(before).Count(),
            }));
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }
    }

    private static async Task<IResult> Install(IFormFile file, [FromForm] string archiveSha256, [FromForm] string? previousSha256, [FromForm] bool rightsConfirmed, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!rightsConfirmed)
        {
            return Invalid("Bitte Lizenz- und Weitergaberechte innerhalb dieser Instanz ausdrücklich bestätigen. Kontometadaten und vertrauliche Inhalte dürfen nicht im Paket stehen.");
        }

        try
        {
            var package = await CatalogPackageEndpoints.Read(file, ct);
            CatalogPackageImporter.PrepareImages(package);
            var hash = Hash(package.Archive);
            if (hash != archiveSha256)
            {
                return Invalid("Die Datei stimmt nicht mit der bestätigten Vorschau überein.");
            }

            return await Store(package, previousSha256, db, user, ct);
        }
        catch (InvalidDataException error)
        {
            return Invalid(error.Message);
        }
    }

    private static async Task<IResult> Availability(Guid id, AvailabilityInput input, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!input.Confirmed)
        {
            return Invalid("Bitte die Änderung bestätigen. Bereits importierte private Fragen, Bearbeitungen und Lernstände bleiben erhalten; ältere Paketfassungen bleiben als Nachweis gespeichert.");
        }

        var packageId = await db.InstanceCatalogPackages.Where(item => item.Id == id).Select(item => item.PackageId).SingleOrDefaultAsync(ct);
        if (packageId == null)
        {
            return Results.NotFound();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"instance-catalog:" + packageId}))", ct);
        var record = await db.InstanceCatalogPackages.SingleAsync(item => item.Id == id, ct);
        if (record.ArchiveSha256 != input.ArchiveSha256)
        {
            return Results.Conflict();
        }

        var wasAvailable = record.Available;
        if (input.Available)
        {
            var previous = await db.InstanceCatalogPackages.Where(item => item.PackageId == packageId && item.Available).ToListAsync(ct);
            foreach (var item in previous)
            {
                item.Available = false;
            }
        }

        Audit(db, user, "catalog-package-availability", id, wasAvailable.ToString(), input.Available.ToString());
        record.Available = input.Available;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Remove(Guid id, [FromBody] AvailabilityInput input, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!input.Confirmed)
        {
            return Invalid("Bitte Entfernung bestätigen: diese Instanzdatei wird gelöscht, private Kopien und Lernstände bleiben erhalten. Eine erneute Bereitstellung benötigt eine neue Inhaltsversion.");
        }

        var packageId = await db.InstanceCatalogPackages.Where(item => item.Id == id).Select(item => item.PackageId).SingleOrDefaultAsync(ct);
        if (packageId == null)
        {
            return Results.NotFound();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"instance-catalog:" + packageId}))", ct);
        var record = await db.InstanceCatalogPackages.SingleAsync(item => item.Id == id, ct);
        if (record.ArchiveSha256 != input.ArchiveSha256)
        {
            return Results.Conflict();
        }

        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = AccountIdentity.TryGetAccountId(user, out var actor) ? actor : null,
            Action = "catalog-package-delete",
            Target = Hash(Encoding.UTF8.GetBytes(record.PackageId + "\n" + record.CatalogVersion)),
            PreviousValue = record.ArchiveSha256,
        });
        db.InstanceCatalogPackages.Remove(record);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }

    private static object Metadata(InstanceCatalogPackage record)
    {
        var package = CatalogPackageReader.Read(record.Archive);
        return new
        {
            record.Id,
            record.PackageId,
            record.CatalogVersion,
            record.ArchiveSha256,
            record.Available,
            record.InstalledAtUtc,
            Title = package.Manifest.GetProperty("title").GetString(),
            Description = package.Manifest.GetProperty("description").GetString(),
            Language = package.Manifest.GetProperty("language").GetString(),
            Publisher = package.Manifest.GetProperty("publisher").GetString(),
            Audiences = package.Questions.Where(question => question.TryGetProperty("age_band", out _)).Select(question => question.GetProperty("age_band").GetString()).Distinct(StringComparer.Ordinal).ToArray(),
            SourceUrls = package.Questions.Select(question => question.GetProperty("provenance")).Where(provenance => provenance.TryGetProperty("source_url", out _)).Select(provenance => provenance.GetProperty("source_url").GetString()).Distinct(StringComparer.Ordinal).ToArray(),
            SourceRevision = package.Manifest.GetProperty("source_revision").GetString(),
            QuestionCount = package.Questions.Count,
            ArchiveBytes = record.Archive.Length,
            ExpandedBytes = package.Files.Values.Sum(bytes => (long)bytes.Length),
            Notices = NoticeFiles.ToDictionary(name => name, name => Encoding.UTF8.GetString(package.Files[name]), StringComparer.Ordinal),
        };
    }

    private static void Audit(LearnPipDbContext db, ClaimsPrincipal user, string action, Guid id, string? previous, string current) =>
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent { ActorAccountId = AccountIdentity.TryGetAccountId(user, out var actor) ? actor : null, Action = action, Target = id.ToString(), PreviousValue = previous, NewValue = current });

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static IResult Invalid(string message) => Results.ValidationProblem(new Dictionary<string, string[]> { ["package"] = [message] });

    /// <summary>Bestätigter Wechsel einer unveränderlichen Paketfassung.</summary>
    /// <param name="Available">Die gewünschte Verfügbarkeit.</param>
    /// <param name="Confirmed">Die ausdrücklich bestätigten Auswirkungen.</param>
    /// <param name="ArchiveSha256">Der angezeigte Paketfingerabdruck.</param>
    public sealed record AvailabilityInput(bool Available, bool Confirmed, string ArchiveSha256);
}
