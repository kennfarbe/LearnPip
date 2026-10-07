// <copyright file="AdministrationEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

/// <summary>
/// Registriert HTTP-Endpunkte für die Verwaltung von Konten, Rollen und Systemupdates.
/// </summary>
public static class AdministrationEndpoints
{
    /// <summary>
    /// Registriert HTTP-Endpunkte für die Verwaltung von Konten, Rollen und Systemupdates.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin").WithTags("Administration")
            .RequireAuthorization(ApiPolicies.Admin);
        admin.MapPut(
            "/accounts/{accountId:guid}/roles/{code}",
            (
                Guid accountId,
                string code,
                AdministrationService service,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) => SetSystemRole(accountId, code, true, service, user, cancellationToken))
            .AddEndpointFilter(async (context, next) =>
            {
                if (context.GetArgument<string>(1) == "moderator")
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<LearnPipDbContext>();
                    var rights = await QuestionPermissions.Snapshot(db, "moderator", context.HttpContext.RequestAborted);
                    if (rights.GetValueOrDefault("readPrivate") &&
                        context.HttpContext.Request.Query["privacyConfirmed"] != "true")
                    {
                        return Results.Conflict(new
                        {
                            error = "privacy_confirmation_required",
                            notice = "Die Moderatorenrolle erlaubt Zugriff auf private Fragen der eigenen Instanz. Prüfe Zweck, Datenminimierung, Datenschutzhinweise und Rollenvergabe. Keine Konten, Codes oder Lernstände werden freigegeben. Der Hinweis muss vor Rollenvergabe ausdrücklich bestätigt werden (privacyConfirmed=true).",
                        });
                    }
                }

                return await next(context);
            });
        admin.MapDelete(
            "/accounts/{accountId:guid}/roles/{code}",
            (
                Guid accountId,
                string code,
                AdministrationService service,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) => SetSystemRole(accountId, code, false, service, user, cancellationToken));
        admin.MapPut(
            "/settings/maintenance-notice",
            async (
                MaintenanceNoticeRequest request,
                AdministrationService service,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
        {
            if (!AccountIdentity.TryGetAccountId(user, out var actorId))
            {
                return Results.Forbid();
            }

            var result = await service.SetMaintenanceNoticeAsync(
                    actorId,
                    request.Value ?? string.Empty,
                    cancellationToken);
            return result switch
            {
                "invalid_value" => Results.BadRequest(),
                "forbidden" => Results.Forbid(),
                _ => Results.NoContent(),
            };
        });
        admin.MapGet(
            "/updates",
            async (UpdateService updates, CancellationToken cancellationToken) =>
            Results.Ok(await updates.StatusAsync(cancellationToken)));
        admin.MapPost(
            "/updates/check",
            async (UpdateService updates, CancellationToken cancellationToken) =>
            Results.Ok(await updates.CheckAsync(true, cancellationToken)))
            .RequireRateLimiting("update-admin");
        admin.MapPut(
            "/updates/interval",
            async (
                UpdateIntervalRequest request,
                UpdateService updates,
                CancellationToken cancellationToken) =>
            await updates.SetIntervalAsync(
                request.Interval,
                cancellationToken)
                ? Results.NoContent() : Results.BadRequest(new { error = "invalid_interval" }));
        admin.MapPost(
            "/updates/install",
            async (
                UpdateInstallRequest request,
                UpdateService updates,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
        {
            if (!AccountIdentity.TryGetAccountId(user, out var actorId))
            {
                return Results.Forbid();
            }

            var result = await updates.QueueAsync(actorId, request.Version, cancellationToken);
            return result.Error switch
            {
                null => Results.Accepted($"/api/v1/admin/updates", result.Job),
                "update_in_progress" => Results.Conflict(new { error = result.Error }),
                "invalid_version" or "unverified_release" => Results.BadRequest(new { error = result.Error }),
                _ => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
            };
        }).RequireRateLimiting("update-admin");
        admin.MapGet(
            "/audit",
            async (LearnPipDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.AdministrationAuditEvents.AsNoTracking()
                .OrderByDescending(item => item.CreatedAtUtc).Take(100)
                .Select(item => new
                {
                    item.Id,
                    item.ActorAccountId,
                    item.Action,
                    item.Target,
                    item.PreviousValue,
                    item.NewValue,
                    item.CreatedAtUtc,
                })
                .ToListAsync(cancellationToken)));

        app.MapPut(
            "/api/v1/groups/{groupId:guid}/members/{accountId:guid}/role",
            async (
                Guid groupId,
                Guid accountId,
                GroupRoleRequest request,
                AdministrationService service,
                ClaimsPrincipal user,
                CancellationToken cancellationToken) =>
        {
            if (!AccountIdentity.TryGetAccountId(user, out var actorId))
            {
                return Results.Forbid();
            }

            var result = await service.SetGroupRoleAsync(
                    actorId,
                    groupId,
                    accountId,
                    request.Code,
                    cancellationToken);
            return result switch
            {
                "changed" or "unchanged" => Results.NoContent(),
                "missing_group" or "missing_account" => Results.NotFound(),
                "forbidden" => Results.Forbid(),
                _ => Results.BadRequest(),
            };
        }).WithTags("Groups").RequireAuthorization(ApiPolicies.ActiveAccount);
        return app;
    }

    private static async Task<IResult> SetSystemRole(
        Guid accountId,
        string code,
        bool grant,
        AdministrationService service,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actorId))
        {
            return Results.Forbid();
        }

        var result = await service.SetSystemRoleAsync(actorId, accountId, code, grant, cancellationToken);
        return result switch
        {
            "changed" or "unchanged" => Results.NoContent(),
            "missing_account" => Results.NotFound(),
            "last_admin" => Results.Conflict(new { error = "last_admin" }),
            "forbidden" => Results.Forbid(),
            _ => Results.BadRequest(),
        };
    }
}
