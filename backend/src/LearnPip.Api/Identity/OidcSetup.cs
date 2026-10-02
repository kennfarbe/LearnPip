// <copyright file="OidcSetup.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

public static class OidcSetup
{
    public const string Scheme = "LearnPipOidc";
    public const string LinkSessionKey = "link_session_id";

    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Oidc:Authority"]) &&
        !string.IsNullOrWhiteSpace(configuration["Oidc:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["Oidc:ClientSecret"]);

    public static AuthenticationBuilder AddLearnPipOidc(
        this AuthenticationBuilder authentication, IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return authentication;
        }

        var authority = configuration["Oidc:Authority"]!.TrimEnd('/');
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Oidc:Authority must be an HTTPS URL.");
        }

        return authentication
            .AddCookie("oidc-temporary", options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddOpenIdConnect(Scheme, options =>
            {
                options.Authority = authority;
                options.ClientId = configuration["Oidc:ClientId"]!;
                options.ClientSecret = configuration["Oidc:ClientSecret"]!;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.SignInScheme = "oidc-temporary";
                options.CallbackPath = "/signin-oidc";
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Events.OnTicketReceived = async context =>
                {
                    context.HandleResponse();
                    var subject = context.Principal?.FindFirst("sub")?.Value;
                    if (string.IsNullOrWhiteSpace(subject))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    Guid? linkingAccountId = null;
                    if (context.Properties?.Items.TryGetValue(LinkSessionKey, out var value) == true)
                    {
                        if (!Guid.TryParse(value, out var sessionId))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }

                        var dbContext = context.HttpContext.RequestServices
                            .GetRequiredService<LearnPipDbContext>();
                        var now = DateTimeOffset.UtcNow;
                        linkingAccountId = await dbContext.AccountSessions.AsNoTracking()
                            .Where(session => session.Id == sessionId &&
                                              session.RevokedAtUtc == null &&
                                              session.ExpiresAtUtc > now &&
                                              session.Account.DeletedAtUtc == null)
                            .Select(session => (Guid?)session.AccountId)
                            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                        if (!linkingAccountId.HasValue)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                    }

                    var identity = context.HttpContext.RequestServices.GetRequiredService<IdentityService>();
                    try
                    {
                        var accountId = await identity.ResolveOidcAsync(
                            authority, subject, linkingAccountId, context.HttpContext.RequestAborted);
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<SessionService>();
                        var grant = await sessions.CreateAsync(accountId, context.HttpContext.RequestAborted);
                        SessionAuthentication.SetCookie(context.HttpContext, grant.Token, grant.ExpiresAtUtc);
                        var path = configuration["Oidc:PostLoginPath"];
                        context.Response.Redirect(IsSafePath(path) ? path! : "/");
                    }
                    catch (IdentityConflictException)
                    {
                        context.Response.StatusCode = StatusCodes.Status409Conflict;
                    }
                };
                options.Events.OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
            });
    }

    private static bool IsSafePath(string? path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith('/') && !path.StartsWith("//", StringComparison.Ordinal) &&
        !path.Contains('\r') && !path.Contains('\n');
}
