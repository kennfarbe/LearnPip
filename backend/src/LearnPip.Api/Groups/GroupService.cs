using System.Security.Cryptography;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Groups;

public sealed class GroupService(LearnPipDbContext db)
{
    public Task<bool> CanManageAsync(Guid groupId, Guid accountId, CancellationToken cancellationToken) =>
        db.StudyGroups.AsNoTracking().AnyAsync(group => group.Id == groupId &&
            group.DeletedAtUtc == null && (group.OwnerAccountId == accountId ||
                group.Memberships.Any(member => member.AccountId == accountId &&
                    member.RoleDefinition.Scope == "group" && member.RoleDefinition.Code == "leader")),
            cancellationToken);

    public Task<bool> CanReadAsync(Guid groupId, Guid accountId, CancellationToken cancellationToken) =>
        db.StudyGroups.AsNoTracking().AnyAsync(group => group.Id == groupId &&
            group.DeletedAtUtc == null && (group.OwnerAccountId == accountId ||
                group.Memberships.Any(member => member.AccountId == accountId)), cancellationToken);

    public async Task<(GroupInvitation? Invitation, string? Code)> CreateInvitationAsync(
        Guid groupId, Guid actorId, DateTimeOffset expiresAtUtc, int maxUses,
        CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(groupId, actorId, cancellationToken)) return (null, null);
        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var invitation = new GroupInvitation
        {
            StudyGroupId = groupId,
            CreatedByAccountId = actorId,
            CodeHash = Hash(code),
            ExpiresAtUtc = expiresAtUtc,
            MaxUses = maxUses
        };
        db.GroupInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);
        return (invitation, code);
    }

    public async Task<string> JoinAsync(Guid actorId, string? code, CancellationToken cancellationToken)
    {
        if (code is not { Length: 43 } || code.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
            return "invalid";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var hash = Hash(code);
        var invitation = await db.GroupInvitations.FromSqlInterpolated(
                $"SELECT * FROM \"GroupInvitations\" WHERE \"CodeHash\" = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (invitation == null || invitation.RevokedAtUtc != null || invitation.ExpiresAtUtc <= now ||
            invitation.UsedCount >= invitation.MaxUses || invitation.StudyGroupId == Guid.Empty ||
            !await db.StudyGroups.AnyAsync(group => group.Id == invitation.StudyGroupId &&
                group.DeletedAtUtc == null, cancellationToken)) return "invalid";
        if (await db.GroupMemberships.AnyAsync(member => member.StudyGroupId == invitation.StudyGroupId &&
                member.AccountId == actorId, cancellationToken) ||
            await db.StudyGroups.AnyAsync(group => group.Id == invitation.StudyGroupId &&
                group.OwnerAccountId == actorId, cancellationToken)) return "already_member";

        // A row lock on the invitation serializes capacity checks. Role creation is idempotent.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Roles\" (\"Id\", \"Scope\", \"Code\", \"Name\") VALUES ({Guid.NewGuid()}, 'group', 'member', 'Group member') ON CONFLICT (\"Scope\", \"Code\") DO NOTHING",
            cancellationToken);
        var roleId = await db.Roles.Where(role => role.Scope == "group" && role.Code == "member")
            .Select(role => role.Id).SingleAsync(cancellationToken);
        db.GroupMemberships.Add(new GroupMembership
        {
            StudyGroupId = invitation.StudyGroupId,
            AccountId = actorId,
            RoleDefinitionId = roleId
        });
        invitation.UsedCount++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return "joined";
    }

    public async Task<bool> RevokeInvitationAsync(Guid groupId, Guid invitationId, Guid actorId,
        CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(groupId, actorId, cancellationToken)) return false;
        var count = await db.GroupInvitations.Where(invitation => invitation.Id == invitationId &&
                invitation.StudyGroupId == groupId && invitation.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(invitation => invitation.RevokedAtUtc,
                DateTimeOffset.UtcNow), cancellationToken);
        return count == 1;
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid targetId, Guid actorId,
        CancellationToken cancellationToken)
    {
        if (actorId != targetId && !await CanManageAsync(groupId, actorId, cancellationToken))
            return false;
        // The owner cannot leave; management must transfer ownership before closing the group.
        if (await db.StudyGroups.AnyAsync(group => group.Id == groupId &&
                group.OwnerAccountId == targetId, cancellationToken)) return false;
        return await db.GroupMemberships.Where(member => member.StudyGroupId == groupId &&
            member.AccountId == targetId).ExecuteDeleteAsync(cancellationToken) == 1;
    }

    private static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));
}
