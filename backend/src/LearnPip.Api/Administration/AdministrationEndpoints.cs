using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

public static class AdministrationEndpoints
{
    public static IEndpointRouteBuilder MapAdministrationEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin").WithTags("Administration")
            .RequireAuthorization(ApiPolicies.Admin);
        admin.MapPut("/accounts/{accountId:guid}/roles/{code}",
            (Guid accountId, string code, AdministrationService service, ClaimsPrincipal user,
                CancellationToken cancellationToken) => SetSystemRole(accountId, code, true, service, user, cancellationToken));
        admin.MapDelete("/accounts/{accountId:guid}/roles/{code}",
            (Guid accountId, string code, AdministrationService service, ClaimsPrincipal user,
                CancellationToken cancellationToken) => SetSystemRole(accountId, code, false, service, user, cancellationToken));
        admin.MapPut("/settings/maintenance-notice", async (MaintenanceNoticeRequest request,
            AdministrationService service, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            if (!AccountIdentity.TryGetAccountId(user, out var actorId)) return Results.Forbid();
            var result = await service.SetMaintenanceNoticeAsync(actorId, request.Value ?? string.Empty,
                cancellationToken);
            return result switch
            {
                "invalid_value" => Results.BadRequest(),
                "forbidden" => Results.Forbid(),
                _ => Results.NoContent()
            };
        });
        admin.MapGet("/audit", async (LearnPipDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.AdministrationAuditEvents.AsNoTracking()
                .OrderByDescending(item => item.CreatedAtUtc).Take(100)
                .Select(item => new { item.Id, item.ActorAccountId, item.Action, item.Target,
                    item.PreviousValue, item.NewValue, item.CreatedAtUtc })
                .ToListAsync(cancellationToken)));

        app.MapPut("/api/v1/groups/{groupId:guid}/members/{accountId:guid}/role", async (
            Guid groupId, Guid accountId, GroupRoleRequest request, AdministrationService service,
            ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            if (!AccountIdentity.TryGetAccountId(user, out var actorId)) return Results.Forbid();
            var result = await service.SetGroupRoleAsync(actorId, groupId, accountId, request.Code,
                cancellationToken);
            return result switch
            {
                "changed" or "unchanged" => Results.NoContent(),
                "missing_group" or "missing_account" => Results.NotFound(),
                "forbidden" => Results.Forbid(),
                _ => Results.BadRequest()
            };
        }).WithTags("Groups").RequireAuthorization(ApiPolicies.ActiveAccount);
        return app;
    }

    private static async Task<IResult> SetSystemRole(Guid accountId, string code, bool grant,
        AdministrationService service, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actorId)) return Results.Forbid();
        var result = await service.SetSystemRoleAsync(actorId, accountId, code, grant, cancellationToken);
        return result switch
        {
            "changed" or "unchanged" => Results.NoContent(),
            "missing_account" => Results.NotFound(),
            "last_admin" => Results.Conflict(new { error = "last_admin" }),
            "forbidden" => Results.Forbid(),
            _ => Results.BadRequest()
        };
    }
}

public sealed record GroupRoleRequest(string Code);
public sealed record MaintenanceNoticeRequest(string? Value);
