// <copyright file="SessionAuthenticationHandler.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnPip.Api.Identity;

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    LearnPipDbContext dbContext) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token;
        if (this.Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            var header = authorization.ToString();
            token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..]
                : null;
        }
        else
        {
            token = this.Request.Cookies[SessionAuthentication.CookieName];
        }

        // 32 random bytes encoded with base64url. Reject large or malformed input before database access.
        if (token is not { Length: 43 } ||
            token.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = SessionAuthentication.Hash(token);
        var now = DateTimeOffset.UtcNow;
        var session = await dbContext.AccountSessions.AsNoTracking()
            .Where(item => item.TokenHash == hash &&
                           item.RevokedAtUtc == null && item.ExpiresAtUtc > now &&
                           item.Account.DeletedAtUtc == null && item.Account.DisabledAtUtc == null)
            .Select(item => new { item.Id, item.AccountId })
            .SingleOrDefaultAsync(this.Context.RequestAborted);
        if (session == null)
        {
            return AuthenticateResult.Fail("Session is invalid or revoked.");
        }

        var claims = new[]
        {
            new Claim(AccountIdentity.AccountIdClaim, session.AccountId.ToString()),
            new Claim(SessionAuthentication.SessionIdClaim, session.Id.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SessionAuthentication.Scheme));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SessionAuthentication.Scheme));
    }
}
