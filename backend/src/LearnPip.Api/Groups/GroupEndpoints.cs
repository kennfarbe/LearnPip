// <copyright file="GroupEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Groups;

public static class GroupEndpoints
{
    public static IEndpointRouteBuilder MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var groups = app.MapGroup("/api/v1/groups").WithTags("Groups")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        groups.MapGet("/", List);
        groups.MapPost("/", Create);
        groups.MapPost("/join", Join).RequireRateLimiting("group-join");
        groups.MapGet("/{id:guid}/members", Members);
        groups.MapDelete("/{id:guid}/members/{accountId:guid}", RemoveMember);
        groups.MapPost("/{id:guid}/invitations", Invite);
        groups.MapDelete("/{id:guid}/invitations/{invitationId:guid}", Revoke);
        groups.MapGet("/{id:guid}/catalogs", Catalogs);
        groups.MapPut("/{id:guid}/catalogs/{catalogId:guid}", ShareCatalog);
        groups.MapDelete("/{id:guid}/catalogs/{catalogId:guid}", UnshareCatalog);
        return app;
    }

    private static async Task<IResult> List(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var items = await db.StudyGroups.AsNoTracking().Where(group => group.DeletedAtUtc == null &&
                (group.OwnerAccountId == accountId ||
                 group.Memberships.Any(member => member.AccountId == accountId)))
            .OrderBy(group => group.Name)
            .Select(group => new GroupView(group.Id, group.Name, group.OwnerAccountId))
            .ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<IReadOnlyList<GroupView>>(items));
    }

    private static async Task<IResult> Create(GroupInput input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160) return Results.BadRequest();
        var group = new StudyGroup { Name = name, OwnerAccountId = accountId };
        db.StudyGroups.Add(group);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/groups/{group.Id}",
            new ApiResponse<GroupView>(new GroupView(group.Id, group.Name, accountId)));
    }

    private static async Task<IResult> Invite(Guid id, InvitationInput input, GroupService service,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        if (input.MaxUses is < 1 or > 1000 || input.ExpiresAtUtc <= now ||
            input.ExpiresAtUtc > now.AddDays(30)) return Results.BadRequest();
        var (invitation, code) = await service.CreateInvitationAsync(id, accountId,
            input.ExpiresAtUtc, input.MaxUses, cancellationToken);
        if (invitation == null || code == null) return Results.NotFound();
        // The plaintext code is shown once and is never stored by the server.
        return Results.Created($"/api/v1/groups/{id}/invitations/{invitation.Id}",
            new ApiResponse<InvitationView>(new InvitationView(invitation.Id, code,
                invitation.ExpiresAtUtc, invitation.MaxUses)));
    }

    private static async Task<IResult> Join(JoinInput input, GroupService service,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        return await service.JoinAsync(accountId, input.Code, cancellationToken) switch
        {
            "joined" => Results.NoContent(),
            "already_member" => Results.Conflict(),
            _ => Results.NotFound()
        };
    }

    private static async Task<IResult> Revoke(Guid id, Guid invitationId, GroupService service,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        return await service.RevokeInvitationAsync(id, invitationId, accountId, cancellationToken)
            ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> Members(Guid id, LearnPipDbContext db, GroupService service,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!await service.CanReadAsync(id, accountId, cancellationToken)) return Results.NotFound();
        var members = await db.GroupMemberships.AsNoTracking()
            .Where(member => member.StudyGroupId == id)
            .OrderBy(member => member.JoinedAtUtc)
            .Select(member => new
            {
                member.AccountId,
                Role = member.RoleDefinition.Code,
                member.JoinedAtUtc
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(members));
    }

    private static async Task<IResult> RemoveMember(Guid id, Guid accountId,
        GroupService service, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actorId)) return Results.Unauthorized();
        return await service.RemoveMemberAsync(id, accountId, actorId, cancellationToken)
            ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> Catalogs(Guid id, LearnPipDbContext db,
        GroupService service, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!await service.CanReadAsync(id, accountId, cancellationToken)) return Results.NotFound();
        var catalogs = await db.GroupCatalogShares.AsNoTracking()
            .Where(share => share.StudyGroupId == id)
            .OrderBy(share => share.PrivateCatalog.Name)
            .Select(share => new { Id = share.PrivateCatalogId, share.PrivateCatalog.Name })
            .ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(catalogs));
    }

    private static async Task<IResult> ShareCatalog(Guid id, Guid catalogId, LearnPipDbContext db,
        GroupService service, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!await service.CanManageAsync(id, accountId, cancellationToken) ||
            !await db.PrivateCatalogs.AnyAsync(catalog => catalog.Id == catalogId &&
                catalog.OwnerAccountId == accountId, cancellationToken)) return Results.NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await db.GroupCatalogShares.AnyAsync(share => share.StudyGroupId == id &&
                share.PrivateCatalogId == catalogId, cancellationToken))
        {
            db.GroupCatalogShares.Add(new GroupCatalogShare
            {
                StudyGroupId = id,
                PrivateCatalogId = catalogId,
                SharedByAccountId = accountId
            });
        }
        var latest = await db.Questions.AsNoTracking()
            .Where(question => question.PrivateCatalogId == catalogId &&
                question.OwnerAccountId == accountId && question.DeletedAtUtc == null)
            .Select(question => question.Versions.OrderByDescending(version => version.VersionNumber)
                .Select(version => (Guid?)version.Id).FirstOrDefault())
            .Where(versionId => versionId != null)
            .Select(versionId => versionId!.Value).ToListAsync(cancellationToken);
        var existing = await db.GroupVersionShares.AsNoTracking()
            .Where(share => share.StudyGroupId == id && latest.Contains(share.QuestionVersionId))
            .Select(share => share.QuestionVersionId).ToListAsync(cancellationToken);
        foreach (var versionId in latest.Except(existing)) db.GroupVersionShares.Add(new GroupVersionShare
        {
            StudyGroupId = id,
            PrivateCatalogId = catalogId,
            QuestionVersionId = versionId
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> UnshareCatalog(Guid id, Guid catalogId,
        LearnPipDbContext db, GroupService service, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!await service.CanManageAsync(id, accountId, cancellationToken)) return Results.NotFound();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var removed = await db.GroupCatalogShares.Where(share => share.StudyGroupId == id &&
                share.PrivateCatalogId == catalogId && share.SharedByAccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);
        if (removed == 1) await db.GroupVersionShares.Where(share => share.StudyGroupId == id &&
            share.PrivateCatalogId == catalogId).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return removed == 1 ? Results.NoContent() : Results.NotFound();
    }
}
