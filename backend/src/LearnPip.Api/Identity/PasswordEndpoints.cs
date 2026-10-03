// <copyright file="PasswordEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;

namespace LearnPip.Api.Identity;

/// <summary>Registriert Passwortanmeldung und Passwortänderung ohne öffentlichen Bootstrap.</summary>
public static class PasswordEndpoints
{
    /// <summary>Registriert die lokalen Passwortendpunkte.</summary>
    /// <param name="app">Der Routen-Builder.</param>
    /// <returns>Die Anwendung mit registrierten Passwortendpunkten.</returns>
    public static IEndpointRouteBuilder MapPasswordEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/password", SignIn).RequireRateLimiting("auth");
        app.MapPost("/api/v1/auth/password/change", Change)
            .RequireAuthorization(ApiPolicies.ActiveAccount).RequireRateLimiting("auth");
        return app;
    }

    private static async Task<IResult> SignIn(PasswordLoginRequest request, PasswordService passwords, HttpContext context, CancellationToken cancellationToken)
    {
        var grant = await passwords.SignInAsync(request.Username, request.Password, cancellationToken);
        if (grant == null)
        {
            return Results.Unauthorized();
        }

        SessionAuthentication.SetCookie(context, grant.Token, grant.ExpiresAtUtc);
        return Results.Ok(new ApiResponse<SessionGrant>(grant));
    }

    private static async Task<IResult> Change(PasswordChangeRequest request, PasswordService passwords, ClaimsPrincipal principal, HttpContext context, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(principal, out var accountId) ||
            !SessionAuthentication.TryGetSessionId(principal, out var sessionId) ||
            !await passwords.ChangeAsync(accountId, sessionId, request.CurrentPassword, request.NewPassword, cancellationToken))
        {
            return Results.Problem("Passwortänderung fehlgeschlagen. Bisheriges Passwort und Anforderungen prüfen.", statusCode: StatusCodes.Status400BadRequest);
        }

        SessionAuthentication.ClearCookie(context);
        return Results.NoContent();
    }
}
