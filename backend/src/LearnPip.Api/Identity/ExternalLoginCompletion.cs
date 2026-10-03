// <copyright file="ExternalLoginCompletion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

/// <summary>
/// Completes an authenticated external login without merging accounts by email.
/// </summary>
public static class ExternalLoginCompletion
{
    /// <summary>Completes sign-in or explicit linking after provider authentication.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="properties">Protected authentication properties.</param>
    /// <param name="issuer">Fixed and validated provider identity.</param>
    /// <param name="subject">Verified stable provider subject.</param>
    /// <param name="postLoginPath">Relative post-login destination.</param>
    /// <returns>The completion task.</returns>
    public static async Task CompleteAsync(
        HttpContext context,
        AuthenticationProperties? properties,
        string issuer,
        string subject,
        string? postLoginPath)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        Guid? linkingAccountId = null;
        if (properties?.Items.TryGetValue(OidcSetup.LinkSessionKey, out var value) == true)
        {
            if (!Guid.TryParse(value, out var sessionId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var dbContext = context.RequestServices.GetRequiredService<LearnPipDbContext>();
            var now = DateTimeOffset.UtcNow;
            linkingAccountId = await dbContext.AccountSessions.AsNoTracking()
                .Where(session => session.Id == sessionId &&
                                  session.RevokedAtUtc == null &&
                                  session.ExpiresAtUtc > now &&
                                  session.Account.DeletedAtUtc == null)
                .Select(session => (Guid?)session.AccountId)
                .SingleOrDefaultAsync(context.RequestAborted);
            if (!linkingAccountId.HasValue)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        try
        {
            var identity = context.RequestServices.GetRequiredService<IdentityService>();
            var accountId = await identity.ResolveOidcAsync(
                issuer,
                subject,
                linkingAccountId,
                context.RequestAborted);
            var sessions = context.RequestServices.GetRequiredService<SessionService>();
            var grant = await sessions.CreateAsync(accountId, context.RequestAborted);
            SessionAuthentication.SetCookie(context, grant.Token, grant.ExpiresAtUtc);
            context.Response.Redirect(IsSafePath(postLoginPath) ? postLoginPath! : "/");
        }
        catch (IdentityConflictException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
        }
    }

    private static bool IsSafePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) &&
        !path.Contains('\r') && !path.Contains('\n');
}
