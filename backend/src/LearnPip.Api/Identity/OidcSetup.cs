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
        HasProviderAuthority(name, configuration[$"Oidc:Providers:{name}:Authority"]) &&
        !string.IsNullOrWhiteSpace(configuration[$"Oidc:Providers:{name}:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration[$"Oidc:Providers:{name}:ClientSecret"]);

    private static bool HasProviderAuthority(string name, string? authority)
    {
        if (!HasHttpsAuthority(authority) ||
            !Uri.TryCreate(authority, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (name == "apple")
        {
            return uri.Host.Equals("appleid.apple.com", StringComparison.OrdinalIgnoreCase) &&
                uri.IsDefaultPort && uri.AbsolutePath is "/";
        }

        if (name == "microsoft")
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return uri.Host.Equals("login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) &&
                uri.IsDefaultPort && segments.Length == 2 &&
                segments[1].Equals("v2.0", StringComparison.Ordinal) &&
                segments[0] is not ("common" or "organizations" or "consumers");
        }

        return false;
    }

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
                if (scheme == Scheme + "-apple")
                {
                    // Apple delivers its web authorization response via cross-site form POST.
                    options.ResponseMode = "form_post";
                    options.CorrelationCookie.SameSite = SameSiteMode.None;
                    options.NonceCookie.SameSite = SameSiteMode.None;
                }
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

                    await ExternalLoginCompletion.CompleteAsync(
                        context.HttpContext,
                        context.Properties,
                        authority,
                        subject,
                        configuration[section + ":PostLoginPath"]);
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
