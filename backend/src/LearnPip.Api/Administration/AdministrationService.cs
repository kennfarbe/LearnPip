using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

public sealed class AdministrationService(LearnPipDbContext db)
{
    private const string BootstrapKey = "admin_bootstrapped";

    private async Task LockAsync(CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1049071810)", cancellationToken);

    private async Task<RoleDefinition> RoleAsync(string scope, string code, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(item => item.Scope == scope && item.Code == code,
            cancellationToken);
        if (role != null) return role;
        role = new RoleDefinition
        {
            Scope = scope,
            Code = code,
            Name = code switch
            {
                "user" => "User",
                "moderator" => "Moderator",
                "admin" => "Administrator",
                "leader" => "Group leader",
                _ => "Group member"
            }
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return role;
    }

    public async Task BootstrapAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        if (await db.SystemSettings.AnyAsync(item => item.Key == BootstrapKey, cancellationToken) ||
            await db.AccountRoles.AnyAsync(item => item.RoleDefinition.Scope == "system" &&
                item.RoleDefinition.Code == "admin", cancellationToken))
            throw new InvalidOperationException("Administrator bootstrap has already been used.");
        if (!await db.Accounts.AnyAsync(item => item.Id == accountId && item.DeletedAtUtc == null,
                cancellationToken))
            throw new InvalidOperationException("The target account does not exist or is deleted.");
        var role = await RoleAsync("system", "admin", cancellationToken);
        db.AccountRoles.Add(new AccountRole { AccountId = accountId, RoleDefinitionId = role.Id });
        db.SystemSettings.Add(new SystemSetting { Key = BootstrapKey, Value = "true" });
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            Action = "admin.bootstrap",
            Target = $"account:{accountId}",
            NewValue = "admin"
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task<bool> IsAdminAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.AccountRoles.AnyAsync(item => item.AccountId == accountId &&
            item.Account.DeletedAtUtc == null && item.RoleDefinition.Scope == "system" &&
            item.RoleDefinition.Code == "admin", cancellationToken);

    public async Task<string> SetSystemRoleAsync(Guid actorId, Guid targetId, string code, bool grant,
        CancellationToken cancellationToken)
    {
        if (code is not ("moderator" or "admin")) return "invalid_role";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        if (!await IsAdminAsync(actorId, cancellationToken)) return "forbidden";
        if (!await db.Accounts.AnyAsync(item => item.Id == targetId && item.DeletedAtUtc == null,
                cancellationToken)) return "missing_account";
        var role = grant ? await RoleAsync("system", code, cancellationToken) :
            await db.Roles.SingleOrDefaultAsync(item => item.Scope == "system" && item.Code == code,
                cancellationToken);
        if (role == null) return "unchanged";
        var existing = await db.AccountRoles.SingleOrDefaultAsync(item => item.AccountId == targetId &&
            item.RoleDefinitionId == role.Id, cancellationToken);
        if (grant && existing != null || !grant && existing == null) return "unchanged";
        if (!grant && code == "admin" && !await db.AccountRoles.AnyAsync(item =>
                item.AccountId != targetId && item.RoleDefinition.Scope == "system" &&
                item.RoleDefinition.Code == "admin" && item.Account.DeletedAtUtc == null, cancellationToken))
            return "last_admin";
        if (grant) db.AccountRoles.Add(new AccountRole { AccountId = targetId, RoleDefinitionId = role.Id });
        else db.AccountRoles.Remove(existing!);
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = actorId,
            Action = grant ? "system_role.grant" : "system_role.revoke",
            Target = $"account:{targetId}",
            PreviousValue = grant ? null : code,
            NewValue = grant ? code : null
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return "changed";
    }

    public async Task<string> SetGroupRoleAsync(Guid actorId, Guid groupId, Guid targetId, string code,
        CancellationToken cancellationToken)
    {
        if (code is not ("member" or "leader")) return "invalid_role";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        var group = await db.StudyGroups.SingleOrDefaultAsync(item => item.Id == groupId &&
            item.DeletedAtUtc == null, cancellationToken);
        if (group == null) return "missing_group";
        var canManage = group.OwnerAccountId == actorId ||
            await db.GroupMemberships.AnyAsync(item => item.StudyGroupId == groupId &&
                item.AccountId == actorId && item.RoleDefinition.Scope == "group" &&
                item.RoleDefinition.Code == "leader", cancellationToken);
        if (!canManage) return "forbidden";
        if (!await db.Accounts.AnyAsync(item => item.Id == targetId && item.DeletedAtUtc == null,
                cancellationToken)) return "missing_account";
        // The group owner is always a leader and cannot be demoted through membership changes.
        if (targetId == group.OwnerAccountId) return "forbidden";
        var role = await RoleAsync("group", code, cancellationToken);
        var membership = await db.GroupMemberships.Include(item => item.RoleDefinition)
            .SingleOrDefaultAsync(item => item.StudyGroupId == groupId && item.AccountId == targetId,
                cancellationToken);
        if (membership?.RoleDefinitionId == role.Id) return "unchanged";
        var previous = membership?.RoleDefinition.Code;
        if (membership == null) db.GroupMemberships.Add(new GroupMembership
        {
            StudyGroupId = groupId,
            AccountId = targetId,
            RoleDefinitionId = role.Id
        });
        else membership.RoleDefinitionId = role.Id;
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = actorId,
            Action = "group_role.set",
            Target = $"group:{groupId}:account:{targetId}",
            PreviousValue = previous,
            NewValue = code
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return "changed";
    }

    public async Task<string> SetMaintenanceNoticeAsync(Guid actorId, string value,
        CancellationToken cancellationToken)
    {
        if (value.Length > 1000) return "invalid_value";
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(cancellationToken);
        if (!await IsAdminAsync(actorId, cancellationToken)) return "forbidden";
        var setting = await db.SystemSettings.SingleOrDefaultAsync(item => item.Key == "maintenance_notice",
            cancellationToken);
        if (setting?.Value == value) return "unchanged";
        var previous = setting?.Value;
        if (setting == null) db.SystemSettings.Add(new SystemSetting { Key = "maintenance_notice", Value = value });
        else { setting.Value = value; setting.UpdatedAtUtc = DateTimeOffset.UtcNow; }
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = actorId,
            Action = "setting.change",
            Target = "setting:maintenance_notice",
            PreviousValue = previous,
            NewValue = value
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return "changed";
    }
}
