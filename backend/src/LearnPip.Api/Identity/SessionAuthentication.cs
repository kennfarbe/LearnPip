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

public static class SessionAuthentication
{
    public const string Scheme = "LearnPipSession";
    public const string CookieName = "learnpip_session";
    public const string SessionIdClaim = "learnpip_session_id";

    public static string NewSecret() =>
        Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string secret) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));

    public static bool TryGetSessionId(ClaimsPrincipal principal, out Guid sessionId) =>
        Guid.TryParse(principal.FindFirstValue(SessionIdClaim), out sessionId);

    public static void SetCookie(HttpContext context, string token, DateTimeOffset expiresAtUtc) =>
        context.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAtUtc
        });

    public static void ClearCookie(HttpContext context) =>
        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });
}

public sealed class SessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    LearnPipDbContext dbContext) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token;
        if (Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            var header = authorization.ToString();
            token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..]
                : null;
        }
        else
        {
            token = Request.Cookies[SessionAuthentication.CookieName];
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
                           item.Account.DeletedAtUtc == null)
            .Select(item => new { item.Id, item.AccountId })
            .SingleOrDefaultAsync(Context.RequestAborted);
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

public sealed class SessionService(LearnPipDbContext dbContext)
{
    public async Task<SessionGrant> CreateAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var token = SessionAuthentication.NewSecret();
        var expires = DateTimeOffset.UtcNow.AddDays(30);
        dbContext.AccountSessions.Add(new AccountSession
        {
            AccountId = accountId,
            TokenHash = SessionAuthentication.Hash(token),
            ExpiresAtUtc = expires
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SessionGrant(token, expires);
    }

    public Task<int> RevokeAsync(Guid sessionId, Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.AccountSessions
            .Where(session => session.Id == sessionId && session.AccountId == accountId &&
                              session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                session => session.RevokedAtUtc, DateTimeOffset.UtcNow), cancellationToken);

    public Task<int> RevokeAllAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.AccountSessions
            .Where(session => session.AccountId == accountId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                session => session.RevokedAtUtc, DateTimeOffset.UtcNow), cancellationToken);
}

public sealed record SessionGrant(string Token, DateTimeOffset ExpiresAtUtc);
