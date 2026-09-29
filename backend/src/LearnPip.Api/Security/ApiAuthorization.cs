using System.Security.Claims;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Security;

public static class ApiPolicies
{
    public const string ActiveAccount = nameof(ActiveAccount);
    public const string QuestionRead = nameof(QuestionRead);
    public const string MediaRead = nameof(MediaRead);
    public const string GroupRead = nameof(GroupRead);
    public const string Moderation = nameof(Moderation);
    public const string Admin = nameof(Admin);
}

public static class AccountIdentity
{
    // This claim must come from a validated issuer. An OIDC subject is not an Account.Id.
    public const string AccountIdClaim = "learnpip_account_id";

    public static bool TryGetAccountId(ClaimsPrincipal principal, out Guid accountId) =>
        Guid.TryParse(principal.FindFirstValue(AccountIdClaim), out accountId);
}

public static class ApiAuthorization
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiPolicies.ActiveAccount, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new ActiveAccountRequirement()));
            options.AddPolicy(ApiPolicies.QuestionRead, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new QuestionReadRequirement()));
            options.AddPolicy(ApiPolicies.MediaRead, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new MediaReadRequirement()));
            options.AddPolicy(ApiPolicies.GroupRead, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new GroupReadRequirement()));
            options.AddPolicy(ApiPolicies.Moderation, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new SystemRoleRequirement("moderator")));
            options.AddPolicy(ApiPolicies.Admin, policy =>
                policy.RequireAuthenticatedUser().AddRequirements(new SystemRoleRequirement("admin")).AddRequirements(new FreshAdminSessionRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, ResourceAuthorizationHandler>();
        return services;
    }
}

public sealed record ActiveAccountRequirement : IAuthorizationRequirement;
public sealed record QuestionReadRequirement : IAuthorizationRequirement;
public sealed record MediaReadRequirement : IAuthorizationRequirement;
public sealed record GroupReadRequirement : IAuthorizationRequirement;
public sealed record SystemRoleRequirement(string Code) : IAuthorizationRequirement;
public sealed record FreshAdminSessionRequirement : IAuthorizationRequirement;

public sealed class ResourceAuthorizationHandler(LearnPipDbContext dbContext) :
    IAuthorizationHandler
{
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
                    context.Succeed(requirement);
                    break;
                case QuestionReadRequirement when context.Resource is Question question &&
                    question.DeletedAtUtc == null &&
                    (question.OwnerAccountId == accountId ||
                     await dbContext.GroupQuestionShares.AsNoTracking().AnyAsync(share =>
                         share.QuestionId == question.Id &&
                         share.RevokedAtUtc == null &&
                         share.StudyGroup.DeletedAtUtc == null &&
                         (share.StudyGroup.OwnerAccountId == accountId ||
                          share.StudyGroup.Memberships.Any(member => member.AccountId == accountId)))):
                    context.Succeed(requirement);
                    break;
                case MediaReadRequirement when context.Resource is MediaAsset media &&
                    media.DeletedAtUtc == null && media.OwnerAccountId == accountId &&
                    (media.QuestionVersionId == null ||
                     await dbContext.QuestionVersions.AsNoTracking().AnyAsync(version =>
                         version.Id == media.QuestionVersionId &&
                         version.Question.DeletedAtUtc == null)):
                    context.Succeed(requirement);
                    break;
                case GroupReadRequirement when context.Resource is StudyGroup group &&
                    group.DeletedAtUtc == null &&
                    (group.OwnerAccountId == accountId ||
                     await dbContext.GroupMemberships.AsNoTracking()
                         .AnyAsync(member => member.StudyGroupId == group.Id && member.AccountId == accountId)):
                    context.Succeed(requirement);
                    break;
                case FreshAdminSessionRequirement when
                    SessionAuthentication.TryGetSessionId(context.User, out var sessionId) &&
                    await dbContext.AccountSessions.AsNoTracking().AnyAsync(session =>
                        session.Id == sessionId && session.AccountId == accountId &&
                        session.RevokedAtUtc == null && session.ExpiresAtUtc > DateTimeOffset.UtcNow &&
                        session.CreatedAtUtc >= DateTimeOffset.UtcNow.AddMinutes(-15)):
                    context.Succeed(requirement);
                    break;
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
