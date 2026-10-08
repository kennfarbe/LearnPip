// <copyright file="DataRightsEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

/// <summary>
/// Registriert HTTP-Endpunkte für den Datenexport und die Kontolöschung.
/// </summary>
public static class DataRightsEndpoints
{
    /// <summary>
    /// Registriert die HTTP-Endpunkte für den Datenexport und die Kontolöschung.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapDataRightsEndpoints(this IEndpointRouteBuilder app)
    {
        var data = app.MapGroup("/api/v1/account").WithTags("Account data")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        data.MapGet("/export", Export).RequireRateLimiting("data-export");
        data.MapPost("/delete", Delete).RequireRateLimiting("account-delete");
        return app;
    }

    private static async Task<IResult> Export(
        ClaimsPrincipal user,
        LearnPipDbContext db,
        HttpContext context,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var id))
        {
            return Results.Unauthorized();
        }

        var account = await db.Accounts.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new { item.Id, item.DisplayName, item.AgeBand, item.CreatedAtUtc })
            .SingleOrDefaultAsync(ct);
        if (account == null)
        {
            return Results.NotFound();
        }

        var identities = await db.ExternalIdentities.AsNoTracking()
            .Where(item => item.AccountId == id)
            .Select(item => new { item.Provider, item.Subject }).ToListAsync(ct);
        var catalogs = await db.PrivateCatalogs.AsNoTracking()
            .Where(item => item.OwnerAccountId == id)
            .Select(item => new { item.Id, item.Name, item.Description, item.CreatedAtUtc }).ToListAsync(ct);
        var catalogPackages = await db.CatalogPackageImports.AsNoTracking()
            .Where(item => item.OwnerAccountId == id)
            .Select(item => new { item.Id, item.PackageId, item.CatalogVersion, item.PrivateCatalogId, item.ImportedAtUtc })
            .ToListAsync(ct);
        var questions = await db.Questions.AsNoTracking()
            .Where(item => item.OwnerAccountId == id)
            .Select(item => new { item.Id, item.PrivateCatalogId, CatalogIds = item.CatalogMemberships.Select(membership => membership.CatalogId).ToArray(), item.CreatedAtUtc, item.DeletedAtUtc }).ToListAsync(ct);
        var drafts = await db.QuestionDrafts.AsNoTracking()
            .Where(item => item.Question.OwnerAccountId == id)
            .Select(item => new { item.QuestionId, item.PayloadJson, item.UpdatedAtUtc })
            .ToListAsync(ct);
        var versions = await db.QuestionVersions.AsNoTracking()
            .Where(item => item.Question.OwnerAccountId == id)
            .Select(item => new { item.Id, item.QuestionId, item.VersionNumber, item.Visibility, item.Prompt, item.Explanation, item.Subject, item.Topic, item.Language, item.Source, item.License, item.AuthorAttribution, item.CreatedAtUtc })
            .ToListAsync(ct);
        var answers = await db.AnswerOptions.AsNoTracking()
            .Where(item => item.QuestionVersion.Question.OwnerAccountId == id)
            .Select(item => new { item.QuestionVersionId, item.Text, item.IsCorrect, item.SortOrder }).ToListAsync(ct);
        var sessions = await db.StudySessions.AsNoTracking()
            .Where(item => item.AccountId == id)
            .Select(item => new { item.Id, item.StartedAtUtc, item.CompletedAtUtc })
            .ToListAsync(ct);
        var attempts = await db.StudyAttempts.AsNoTracking()
            .Where(item => item.StudySession.AccountId == id)
            .OrderBy(item => item.AnsweredAtUtc)
            .Select(item => new { item.Id, item.StudySessionId, item.QuestionVersionId, item.QuestionVersion.Subject, item.QuestionVersion.Topic, item.IsCorrect, item.WasGuessed, item.ExplanationViewedAtUtc, item.AnsweredAtUtc })
            .ToListAsync(ct);
        var selections = await db.StudyAttemptSelections.AsNoTracking()
            .Where(item => item.StudyAttempt.StudySession.AccountId == id)
            .Select(item => new { item.StudyAttemptId, item.AnswerOptionId })
            .ToListAsync(ct);
        var media = await db.MediaAssets.AsNoTracking()
            .Where(item => item.OwnerAccountId == id && item.DeletedAtUtc == null)
            .Select(item => new { item.Id, item.MediaType, item.AltText, item.ByteLength, item.CreatedAtUtc })
            .ToListAsync(ct);
        var groups = await db.GroupMemberships.AsNoTracking()
            .Where(item => item.AccountId == id)
            .Select(item => new { item.StudyGroupId, item.RoleDefinitionId })
            .ToListAsync(ct);
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.ContentDisposition = "attachment; filename=learnpip-export.json";
        var notice = "Media bytes can be downloaded individually via /api/v1/media/{id}/content. " +
            "Original catalog ZIPs can be downloaded individually via /api/v1/catalog-packages/{id}/original. Copies published under open licenses by others cannot be recalled.";
        return Results.Json(new { generatedAtUtc = DateTimeOffset.UtcNow, account, identities, catalogs, catalogPackages, questions, drafts, versions, answers, sessions, attempts, selections, media, groups, notice });
    }

    private static async Task<IResult> Delete(
        DeleteAccountRequest input,
        ClaimsPrincipal user,
        LearnPipDbContext db,
        AccountLifecycleService lifecycle,
        HttpContext context,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var id))
        {
            return Results.Unauthorized();
        }

        if (input.Confirmation != "DELETE" || input.RecoverySecret is not { Length: 43 })
        {
            return Results.BadRequest();
        }

        var hash = SessionAuthentication.Hash(input.RecoverySecret);
        if (!await db.RecoveryCredentials.AsNoTracking().AnyAsync(
            item =>
                item.AccountId == id && item.SecretHash == hash,
            ct))
        {
            return Results.Unauthorized();
        }

        if (!await lifecycle.DeleteOwnAsync(id, ct))
        {
            return Results.NotFound();
        }

        SessionAuthentication.ClearCookie(context);
        return Results.NoContent();
    }
}
