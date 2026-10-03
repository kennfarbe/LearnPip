// <copyright file="GroupService.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Cryptography;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Groups;

/// <summary>
/// Verwaltet Lerngruppen, Einladungen und Mitgliedschaften.
/// </summary>
/// <param name="db">Der Datenbankkontext.</param>
public sealed class GroupService(LearnPipDbContext db)
{
    /// <summary>
    /// Prüft, ob ein Konto eine Lerngruppe verwalten darf.
    /// </summary>
    /// <param name="groupId">Die Kennung der Lerngruppe.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<bool> CanManageAsync(Guid groupId, Guid accountId, CancellationToken cancellationToken) =>
        db.StudyGroups.AsNoTracking().AnyAsync(
        group => group.Id == groupId &&
            group.DeletedAtUtc == null && (group.OwnerAccountId == accountId ||
                group.Memberships.Any(member => member.AccountId == accountId &&
                    member.RoleDefinition.Scope == "group" && member.RoleDefinition.Code == "leader")),
        cancellationToken);

    /// <summary>
    /// Prüft, ob ein Konto eine Lerngruppe lesen darf.
    /// </summary>
    /// <param name="groupId">Die Kennung der Lerngruppe.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<bool> CanReadAsync(Guid groupId, Guid accountId, CancellationToken cancellationToken) =>
        db.StudyGroups.AsNoTracking().AnyAsync(
        group => group.Id == groupId &&
            group.DeletedAtUtc == null && (group.OwnerAccountId == accountId ||
                group.Memberships.Any(member => member.AccountId == accountId)),
        cancellationToken);

    /// <summary>
    /// Erstellt eine zeitlich und in der Nutzung begrenzte Gruppeneinladung.
    /// </summary>
    /// <param name="groupId">Die Kennung der Lerngruppe.</param>
    /// <param name="actorId">Die Kennung des ausführenden Kontos.</param>
    /// <param name="expiresAtUtc">Der Ablaufzeitpunkt in UTC.</param>
    /// <param name="maxUses">Die höchstens erlaubte Anzahl von Einlösungen.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<(GroupInvitation? Invitation, string? Code)> CreateInvitationAsync(
        Guid groupId,
        Guid actorId,
        DateTimeOffset expiresAtUtc,
        int maxUses,
        CancellationToken cancellationToken)
    {
        if (!await this.CanManageAsync(groupId, actorId, cancellationToken))
        {
            return (null, null);
        }

        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var invitation = new GroupInvitation
        {
            StudyGroupId = groupId,
            CreatedByAccountId = actorId,
            CodeHash = Hash(code),
            ExpiresAtUtc = expiresAtUtc,
            MaxUses = maxUses,
        };
        db.GroupInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);
        return (invitation, code);
    }

    /// <summary>
    /// Löst einen Einladungscode ein und legt eine Gruppenmitgliedschaft an.
    /// </summary>
    /// <param name="actorId">Die Kennung des ausführenden Kontos.</param>
    /// <param name="code">Der Rollen-, Einladungs- oder Bestätigungscode.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<string> JoinAsync(
        Guid actorId,
        string? code,
        CancellationToken cancellationToken)
    {
        if (code is not { Length: 43 } || code.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
        {
            return "invalid";
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var hash = Hash(code);
        var invitation = await db.GroupInvitations.FromSqlInterpolated(
                $"SELECT * FROM \"GroupInvitations\" WHERE \"CodeHash\" = {hash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (invitation == null || invitation.RevokedAtUtc != null || invitation.ExpiresAtUtc <= now ||
            invitation.UsedCount >= invitation.MaxUses || invitation.StudyGroupId == Guid.Empty ||
            !await db.StudyGroups.AnyAsync(
            group => group.Id == invitation.StudyGroupId &&
                group.DeletedAtUtc == null,
            cancellationToken))
        {
            return "invalid";
        }

        if (await db.GroupMemberships.AnyAsync(
            member => member.StudyGroupId == invitation.StudyGroupId &&
                member.AccountId == actorId,
            cancellationToken) ||
            await db.StudyGroups.AnyAsync(
            group => group.Id == invitation.StudyGroupId &&
                group.OwnerAccountId == actorId,
            cancellationToken))
        {
            return "already_member";
        }

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
            RoleDefinitionId = roleId,
        });
        invitation.UsedCount++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return "joined";
    }

    /// <summary>
    /// Widerruft eine Gruppeneinladung.
    /// </summary>
    /// <param name="groupId">Die Kennung der Lerngruppe.</param>
    /// <param name="invitationId">Die Kennung der Gruppeneinladung.</param>
    /// <param name="actorId">Die Kennung des ausführenden Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<bool> RevokeInvitationAsync(
        Guid groupId,
        Guid invitationId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (!await this.CanManageAsync(groupId, actorId, cancellationToken))
        {
            return false;
        }

        var count = await db.GroupInvitations.Where(invitation => invitation.Id == invitationId &&
                invitation.StudyGroupId == groupId && invitation.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
            setters => setters.SetProperty(
                invitation => invitation.RevokedAtUtc,
                DateTimeOffset.UtcNow),
            cancellationToken);
        return count == 1;
    }

    /// <summary>
    /// Entfernt eine Gruppenmitgliedschaft unter Beachtung der Besitzerrechte.
    /// </summary>
    /// <param name="groupId">Die Kennung der Lerngruppe.</param>
    /// <param name="targetId">Die Kennung des zu ändernden Kontos.</param>
    /// <param name="actorId">Die Kennung des ausführenden Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<bool> RemoveMemberAsync(
        Guid groupId,
        Guid targetId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (actorId != targetId && !await this.CanManageAsync(groupId, actorId, cancellationToken))
        {
            return false;
        }

        // The owner cannot leave; management must transfer ownership before closing the group.
        if (await db.StudyGroups.AnyAsync(
            group => group.Id == groupId &&
                group.OwnerAccountId == targetId,
            cancellationToken))
        {
            return false;
        }

        return await db.GroupMemberships.Where(member => member.StudyGroupId == groupId &&
            member.AccountId == targetId).ExecuteDeleteAsync(cancellationToken) == 1;
    }

    private static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));
}
