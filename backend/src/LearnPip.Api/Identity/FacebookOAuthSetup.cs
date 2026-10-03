// <copyright file="FacebookOAuthSetup.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace LearnPip.Api.Identity;

/// <summary>
/// Separate optional Facebook OAuth login without email-based account merging.
/// </summary>
public static class FacebookOAuthSetup
{
    /// <summary>The Facebook OAuth authentication scheme.</summary>
    public const string Scheme = "LearnPipFacebook";

    /// <summary>Checks whether the Facebook app is configured.</summary>
    /// <param name="configuration">Server configuration.</param>
    /// <returns>Whether complete credentials are present.</returns>
    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["FacebookOAuth:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["FacebookOAuth:ClientSecret"]);

    /// <summary>Registers Facebook Login as a separate server-side OAuth flow.</summary>
    /// <param name="authentication">Authentication builder.</param>
    /// <param name="configuration">Server configuration.</param>
    /// <returns>Authentication builder.</returns>
    public static AuthenticationBuilder AddLearnPipFacebook(
        this AuthenticationBuilder authentication,
        IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return authentication;
        }

        authentication.AddCookie("facebook-temporary", options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        return authentication.AddOAuth(Scheme, options =>
        {
            options.ClientId = configuration["FacebookOAuth:ClientId"]!;
            options.ClientSecret = configuration["FacebookOAuth:ClientSecret"]!;
            options.AuthorizationEndpoint = "https://www.facebook.com/v24.0/dialog/oauth";
            options.TokenEndpoint = "https://graph.facebook.com/v24.0/oauth/access_token";
            options.CallbackPath = "/signin-facebook";
            options.SignInScheme = "facebook-temporary";
            options.SaveTokens = false;
            options.Scope.Clear();
            options.Events.OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    new UriBuilder(Uri.UriSchemeHttps, "graph.facebook.com")
                    {
                        Path = "v24.0/me",
                        Query = "fields=id",
                    }.Uri);
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    context.AccessToken);
                using var response = await context.Backchannel.SendAsync(
                    request,
                    context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                using var payload = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted),
                    cancellationToken: context.HttpContext.RequestAborted);
                if (!payload.RootElement.TryGetProperty("id", out var id) ||
                    id.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(id.GetString()))
                {
                    throw new InvalidOperationException("Facebook did not return a stable app-scoped ID.");
                }

                context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.GetString()!));
            };
            options.Events.OnTicketReceived = async context =>
            {
                context.HandleResponse();
                var subject = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await ExternalLoginCompletion.CompleteAsync(
                    context.HttpContext,
                    context.Properties,
                    "https://facebook.com/app/" + options.ClientId,
                    subject ?? string.Empty,
                    configuration["FacebookOAuth:PostLoginPath"]);
            };
            options.Events.OnRemoteFailure = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
        });
    }
}
