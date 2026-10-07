// <copyright file="ResourceAuthorizationHandler.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Security;

/// <summary>
/// Prüft Konto-, Rollen- und Ressourcenberechtigungen für die API.
/// </summary>
/// <param name="dbContext">Der Datenbankkontext.</param>
public sealed class ResourceAuthorizationHandler(LearnPipDbContext dbContext) :
    IAuthorizationHandler
{
    /// <summary>
    /// Prüft alle unterstützten Ressourcenanforderungen der Autorisierung.
    /// </summary>
    /// <param name="context">Der Kontext der HTTP-Anfrage oder Autorisierungsprüfung.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true ||
            !AccountIdentity.TryGetAccountId(context.User, out var accountId) ||
            !await dbContext.Accounts.AsNoTracking()
                .AnyAsync(account => account.Id == accountId && account.DeletedAtUtc == null &&
                    account.DisabledAtUtc == null))
        {
            return;
        }

        foreach (var requirement in context.PendingRequirements.ToArray())
        {
            switch (requirement)
            {
                case ActiveAccountRequirement:
                case QuestionReadRequirement when context.Resource is Question question &&
                    question.DeletedAtUtc == null &&
                    ((question.OwnerAccountId == accountId && await QuestionPermissions.Allows(dbContext, accountId, "readOwn", CancellationToken.None)) ||
                     await QuestionAccess.ReadableVersions(
                dbContext,
                accountId)
                         .AnyAsync(version => version.QuestionId == question.Id)):
                case MediaReadRequirement when context.Resource is MediaAsset media &&
                    media.DeletedAtUtc == null && ((media.OwnerAccountId == accountId && await QuestionPermissions.Allows(dbContext, accountId, "readOwn", CancellationToken.None)) ||
                     await QuestionAccess.ReadableVersions(
                dbContext,
                accountId)
                         .AnyAsync(version => version.Question.OwnerAccountId == media.OwnerAccountId &&
                             dbContext.QuestionContentBlocks.Any(block => block.MediaAssetId == media.Id &&
                                 (block.QuestionVersionId == version.Id ||
                                  (block.AnswerOption != null &&
                                  block.AnswerOption.QuestionVersionId == version.Id))))) &&
                    (media.QuestionVersionId == null ||
                     await dbContext.QuestionVersions.AsNoTracking().AnyAsync(version =>
                         version.Id == media.QuestionVersionId &&
                         version.Question.DeletedAtUtc == null)):
                case GroupReadRequirement when context.Resource is StudyGroup group &&
                    group.DeletedAtUtc == null &&
                    (group.OwnerAccountId == accountId ||
                     await dbContext.GroupMemberships.AsNoTracking()
                         .AnyAsync(member => member.StudyGroupId == group.Id && member.AccountId == accountId)):
                case FreshIdentitySessionRequirement or FreshAdminSessionRequirement when
                    SessionAuthentication.TryGetSessionId(context.User, out var sessionId) &&
                    await dbContext.AccountSessions.AsNoTracking().AnyAsync(session =>
                        session.Id == sessionId && session.AccountId == accountId &&
                        session.RevokedAtUtc == null && session.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                        session.CreatedAtUtc >= DateTimeOffset.UtcNow.AddMinutes(-15)):
                case SystemRoleRequirement role when
                    await dbContext.AccountRoles.AsNoTracking().AnyAsync(grant =>
                        grant.AccountId == accountId &&
                        grant.RoleDefinition.Scope == "system" &&
                        (grant.RoleDefinition.Code == role.Code ||
                         (role.Code == "moderator" && grant.RoleDefinition.Code == "admin"))):
                    context.Succeed(requirement);
                    break;
            }
        }
    }
}
