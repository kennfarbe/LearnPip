// <copyright file="OidcSetup.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Identity;

/// <summary>
/// Konfiguriert die optionale Anmeldung über OpenID Connect.
/// </summary>
public static class OidcSetup
{
    /// <summary>
    /// Den Namen des Authentifizierungsschemas.
    /// </summary>
    public const string Scheme = "LearnPipOidc";

    /// <summary>
    /// Den Schlüssel der Sitzung zur Identitätsverknüpfung.
    /// </summary>
    public const string LinkSessionKey = "link_session_id";

    private static readonly string[] ProviderNames = ["apple", "microsoft"];

    /// <summary>Returns configured OIDC names without exposing credentials.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>Enabled provider names.</returns>
    public static string[] EnabledProviders(IConfiguration configuration) =>
        ProviderNames.Where(name => IsConfigured(configuration, name)).ToArray();

    /// <summary>Returns the authentication scheme for a validated configured provider.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="name">The allowed provider name.</param>
    /// <returns>The scheme, or null for an unavailable provider.</returns>
    public static string? ProviderScheme(IConfiguration configuration, string name) =>
        ProviderNames.Contains(name, StringComparer.Ordinal) && IsConfigured(configuration, name)
            ? Scheme + "-" + name : null;

    /// <summary>
    /// Prüft, ob die OIDC-Anmeldung vollständig konfiguriert ist.
    /// </summary>
    /// <param name="configuration">Die Anwendungskonfiguration.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["Oidc:Authority"]) &&
        !string.IsNullOrWhiteSpace(configuration["Oidc:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["Oidc:ClientSecret"]);

    /// <summary>
    /// Registriert die optional konfigurierte OIDC-Authentifizierung.
    /// </summary>
    /// <param name="authentication">Die Authentifizierungsregistrierung des Hosts.</param>
    /// <param name="configuration">Die Anwendungskonfiguration.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static AuthenticationBuilder AddLearnPipOidc(
        this AuthenticationBuilder authentication,
        IConfiguration configuration)
    {
        var providers = EnabledProviders(configuration);
        if (providers.Length == 0 && !IsEnabled(configuration))
        {
            return authentication;
        }

        authentication.AddCookie("oidc-temporary", options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
        if (IsEnabled(configuration))
        {
            AddProvider(authentication, configuration, "Oidc", Scheme, "/signin-oidc");
        }

        foreach (var name in providers)
        {
            AddProvider(
                authentication,
                configuration,
                $"Oidc:Providers:{name}",
                Scheme + "-" + name,
                "/signin-oidc-" + name);
        }

        return authentication;
    }

    private static bool IsConfigured(IConfiguration configuration, string name) =>
        HasHttpsAuthority(configuration[$"Oidc:Providers:{name}:Authority"]) &&
        !string.IsNullOrWhiteSpace(configuration[$"Oidc:Providers:{name}:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration[$"Oidc:Providers:{name}:ClientSecret"]);

    private static bool HasHttpsAuthority(string? authority) =>
        Uri.TryCreate(authority, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        string.IsNullOrEmpty(uri.Query) &&
        string.IsNullOrEmpty(uri.Fragment);

    private static void AddProvider(
        AuthenticationBuilder authentication,
        IConfiguration configuration,
        string section,
        string scheme,
        string callback)
    {
        var authority = configuration[section + ":Authority"]!.TrimEnd('/');
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Oidc:Authority must be an HTTPS URL.");
        }

        authentication.AddOpenIdConnect(
            scheme,
            options =>
            {
                options.Authority = authority;
                options.ClientId = configuration[section + ":ClientId"]!;
                options.ClientSecret = configuration[section + ":ClientSecret"]!;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = false;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = true;
                options.SignInScheme = "oidc-temporary";
                options.CallbackPath = callback;
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
                            authority,
                            subject,
                            linkingAccountId,
                            context.HttpContext.RequestAborted);
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<SessionService>();
                        var grant = await sessions.CreateAsync(accountId, context.HttpContext.RequestAborted);
                        SessionAuthentication.SetCookie(context.HttpContext, grant.Token, grant.ExpiresAtUtc);
                        var path = configuration[section + ":PostLoginPath"];
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
