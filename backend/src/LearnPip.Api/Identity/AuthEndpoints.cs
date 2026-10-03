// <copyright file="AuthEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

/// <summary>
/// Registriert HTTP-Endpunkte für die Anmeldung und Kontowiederherstellung.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Registriert HTTP-Endpunkte für die Anmeldung und Kontowiederherstellung.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/v1/auth").WithTags("Identity");

        auth.MapPost(
            "/pseudonymous",
            CreatePseudonymous)
            .RequireRateLimiting("auth")
            .Produces<ApiResponse<NewAccount>>(StatusCodes.Status201Created);
        auth.MapPost(
            "/recovery",
            Recover)
            .RequireRateLimiting("auth")
            .Produces<ApiResponse<SessionGrant>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost(
            "/email/start",
            StartEmail)
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status202Accepted)
            .ProducesValidationProblem();
        auth.MapPost(
            "/email/complete",
            CompleteEmail)
            .RequireRateLimiting("auth")
            .Produces<ApiResponse<SessionGrant>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapGet("/github/start", (IConfiguration configuration) =>
            GithubOAuthSetup.IsEnabled(configuration)
                ? Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [GithubOAuthSetup.Scheme])
                : Results.NotFound())
            .RequireRateLimiting("auth");
        auth.MapGet("/oidc/providers", (IConfiguration configuration) =>
            Results.Ok(OidcSetup.EnabledProviders(configuration)))
            .Produces<string[]>();
        auth.MapGet("/oidc/{provider}/start", StartNamedOidc)
            .RequireRateLimiting("auth");
        auth.MapGet(
            "/oidc/start",
            StartOidc)
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status302Found);

        var secured = auth.MapGroup(string.Empty).RequireAuthorization(ApiPolicies.ActiveAccount);
        secured.MapGet(
            "/me",
            GetMe)
            .Produces<ApiResponse<AccountInfo>>();
        secured.MapGet(
            "/capabilities",
            GetCapabilities)
            .Produces<ApiResponse<ApplicationCapabilities>>();
        secured.MapPost(
            "/logout",
            Logout)
            .Produces(StatusCodes.Status204NoContent);
        secured.MapPost(
            "/logout-all",
            LogoutAll)
            .Produces(StatusCodes.Status204NoContent);
        secured.MapPost(
            "/recovery/rotate",
            RotateRecovery)
            .RequireRateLimiting("auth")
            .Produces<ApiResponse<NewAccount>>();
        secured.MapPost(
            "/email/link/start",
            StartEmailLink)
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status202Accepted);
        secured.MapPost(
            "/email/link/complete",
            CompleteEmailLink)
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status204NoContent);
        secured.MapGet("/oidc/{provider}/link/start", StartNamedOidcLink)
            .RequireRateLimiting("auth");
        secured.MapGet(
            "/oidc/link/start",
            StartOidcLink)
            .RequireRateLimiting("auth")
            .Produces(StatusCodes.Status302Found);
        return app;
    }

    private static async Task<IResult> CreatePseudonymous(
        IdentityService identity,
        SessionService sessions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var created = await identity.CreatePseudonymousAsync(cancellationToken);
        var grant = await sessions.CreateAsync(created.AccountId, cancellationToken);
        SessionAuthentication.SetCookie(context, grant.Token, grant.ExpiresAtUtc);
        return Results.Json(
            new ApiResponse<NewAccount>(
            new NewAccount(created.AccountId, created.Secret, grant)),
            statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> Recover(
        RecoveryRequest request,
        IdentityService identity,
        SessionService sessions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var accountId = await identity.RecoverAsync(request.Secret, cancellationToken);
        if (accountId == null)
        {
            return Results.Unauthorized();
        }

        var grant = await sessions.CreateAsync(accountId.Value, cancellationToken);
        SessionAuthentication.SetCookie(context, grant.Token, grant.ExpiresAtUtc);
        return Results.Ok(new ApiResponse<SessionGrant>(grant));
    }

    private static async Task<IResult> StartEmail(
        EmailStartRequest request,
        IdentityService identity,
        CancellationToken cancellationToken)
    {
        var email = IdentityService.NormalizeEmail(request.Email);
        if (email == null)
        {
            return InvalidEmail();
        }

        if (!identity.EmailEnabled)
        {
            return Results.Problem("Email login is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        await identity.StartEmailCodeAsync(email, "signin", null, null, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> CompleteEmail(
        EmailCompleteRequest request,
        IdentityService identity,
        SessionService sessions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var email = IdentityService.NormalizeEmail(request.Email);
        if (email == null || request.Code == null)
        {
            return Results.Unauthorized();
        }

        try
        {
            var accountId = await identity.CompleteEmailCodeAsync(
                email,
                request.Code,
                "signin",
                null,
                null,
                cancellationToken);
            if (accountId == null)
            {
                return Results.Unauthorized();
            }

            var grant = await sessions.CreateAsync(accountId.Value, cancellationToken);
            SessionAuthentication.SetCookie(context, grant.Token, grant.ExpiresAtUtc);
            return Results.Ok(new ApiResponse<SessionGrant>(grant));
        }
        catch (IdentityConflictException)
        {
            return Results.Conflict();
        }
    }

    private static IResult StartNamedOidc(string provider, IConfiguration configuration)
    {
        var scheme = OidcSetup.ProviderScheme(configuration, provider);
        return scheme == null ? Results.NotFound() :
            Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [scheme]);
    }

    private static IResult StartNamedOidcLink(
        string provider,
        IConfiguration configuration,
        ClaimsPrincipal principal)
    {
        var scheme = OidcSetup.ProviderScheme(configuration, provider);
        if (scheme == null)
        {
            return Results.NotFound();
        }

        if (!SessionAuthentication.TryGetSessionId(principal, out var sessionId))
        {
            return Results.Unauthorized();
        }

        var properties = new AuthenticationProperties { RedirectUri = "/" };
        properties.Items[OidcSetup.LinkSessionKey] = sessionId.ToString();
        return Results.Challenge(properties, [scheme]);
    }

    private static IResult StartOidc(IConfiguration configuration)
    {
        if (!OidcSetup.IsEnabled(configuration))
        {
            return Results.Problem("OIDC is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Challenge(
            new AuthenticationProperties
            {
                RedirectUri = "/",
            },
            [OidcSetup.Scheme]);
    }

    private static async Task<IResult> GetMe(
        ClaimsPrincipal principal,
        LearnPipDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(principal, out var accountId))
        {
            return Results.Unauthorized();
        }

        var account = await dbContext.Accounts.AsNoTracking()
            .Where(item => item.Id == accountId && item.DeletedAtUtc == null)
            .Select(item => new AccountInfo(
                item.Id,
                item.DisplayName,
                item.LastActivityAtUtc,
                item.DisabledAtUtc))
            .SingleAsync(cancellationToken);
        return Results.Ok(new ApiResponse<AccountInfo>(account));
    }

    private static async Task<IResult> GetCapabilities(
        ClaimsPrincipal principal,
        IAuthorizationService authorization)
    {
        var administration = await authorization.AuthorizeAsync(principal, null, ApiPolicies.Admin);
        var moderation = await authorization.AuthorizeAsync(principal, null, ApiPolicies.Moderation);
        return Results.Ok(new ApiResponse<ApplicationCapabilities>(
            new ApplicationCapabilities(administration.Succeeded, moderation.Succeeded)));
    }

    private static async Task<IResult> Logout(
        ClaimsPrincipal principal,
        SessionService sessions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(principal, out var accountId) ||
            !SessionAuthentication.TryGetSessionId(principal, out var sessionId))
        {
            return Results.Unauthorized();
        }

        await sessions.RevokeAsync(sessionId, accountId, cancellationToken);
        SessionAuthentication.ClearCookie(context);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAll(
        ClaimsPrincipal principal,
        SessionService sessions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(principal, out var accountId))
        {
            return Results.Unauthorized();
        }

        await sessions.RevokeAllAsync(accountId, cancellationToken);
        SessionAuthentication.ClearCookie(context);
        return Results.NoContent();
    }

    private static async Task<IResult> RotateRecovery(
        ClaimsPrincipal principal,
        IdentityService identity,
        SessionService sessions,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(principal, out var accountId))
        {
            return Results.Unauthorized();
        }

        var secret = await identity.RotateRecoveryAsync(accountId, cancellationToken);
        await sessions.RevokeAllAsync(accountId, cancellationToken);
        var grant = await sessions.CreateAsync(accountId, cancellationToken);
        SessionAuthentication.SetCookie(context, grant.Token, grant.ExpiresAtUtc);
        return Results.Ok(new ApiResponse<NewAccount>(new NewAccount(accountId, secret, grant)));
    }

    private static async Task<IResult> StartEmailLink(
        EmailStartRequest request,
        ClaimsPrincipal principal,
        IdentityService identity,
        CancellationToken cancellationToken)
    {
        var email = IdentityService.NormalizeEmail(request.Email);
        if (email == null)
        {
            return InvalidEmail();
        }

        if (!identity.EmailEnabled)
        {
            return Results.Problem("Email login is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!AccountIdentity.TryGetAccountId(principal, out var accountId) ||
            !SessionAuthentication.TryGetSessionId(principal, out var sessionId))
        {
            return Results.Unauthorized();
        }

        await identity.StartEmailCodeAsync(email, "link", accountId, sessionId, cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> CompleteEmailLink(
        EmailCompleteRequest request,
        ClaimsPrincipal principal,
        IdentityService identity,
        CancellationToken cancellationToken)
    {
        var email = IdentityService.NormalizeEmail(request.Email);
        if (email == null || request.Code == null ||
            !AccountIdentity.TryGetAccountId(principal, out var accountId) ||
            !SessionAuthentication.TryGetSessionId(principal, out var sessionId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var resolved = await identity.CompleteEmailCodeAsync(
                email,
                request.Code,
                "link",
                accountId,
                sessionId,
                cancellationToken);
            return resolved == null ? Results.Unauthorized() : Results.NoContent();
        }
        catch (IdentityConflictException)
        {
            return Results.Conflict();
        }
    }

    private static IResult StartOidcLink(
        IConfiguration configuration,
        ClaimsPrincipal principal)
    {
        if (!OidcSetup.IsEnabled(configuration))
        {
            return Results.Problem("OIDC is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!SessionAuthentication.TryGetSessionId(principal, out var sessionId))
        {
            return Results.Unauthorized();
        }

        var properties = new AuthenticationProperties { RedirectUri = "/" };
        properties.Items[OidcSetup.LinkSessionKey] = sessionId.ToString();
        return Results.Challenge(properties, [OidcSetup.Scheme]);
    }

    private static IResult InvalidEmail() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["email"] = ["Enter a valid email address."] });
}
