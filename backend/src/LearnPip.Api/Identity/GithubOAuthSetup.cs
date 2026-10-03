// <copyright file="GithubOAuthSetup.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace LearnPip.Api.Identity;

/// <summary>
/// Independent GitHub OAuth web flow; GitHub sign-in is not generic OIDC.
/// </summary>
public static class GithubOAuthSetup
{
    /// <summary>The GitHub authentication scheme.</summary>
    public const string Scheme = "LearnPipGithub";

    /// <summary>Whether GitHub is configured on the server.</summary>
    /// <param name="configuration">Server configuration.</param>
    /// <returns>True only for complete credentials.</returns>
    public static bool IsEnabled(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration["GithubOAuth:ClientId"]) &&
        !string.IsNullOrWhiteSpace(configuration["GithubOAuth:ClientSecret"]);

    /// <summary>Registers GitHub's server-side OAuth authorization-code flow.</summary>
    /// <param name="authentication">Authentication builder.</param>
    /// <param name="configuration">Server configuration.</param>
    /// <returns>Authentication builder.</returns>
    public static AuthenticationBuilder AddLearnPipGithub(
        this AuthenticationBuilder authentication,
        IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return authentication;
        }

        authentication.AddCookie("github-temporary", options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        return authentication.AddOAuth(Scheme, options =>
        {
            options.ClientId = configuration["GithubOAuth:ClientId"]!;
            options.ClientSecret = configuration["GithubOAuth:ClientSecret"]!;
            options.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
            options.TokenEndpoint = "https://github.com/login/oauth/access_token";
            options.CallbackPath = "/signin-github";
            options.SignInScheme = "github-temporary";
            options.UsePkce = true;
            options.SaveTokens = false;
            options.Scope.Clear();
            options.Events.OnCreatingTicket = async context =>
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    new UriBuilder(Uri.UriSchemeHttps, "api.github.com") { Path = "user" }.Uri);
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    context.AccessToken);
                request.Headers.UserAgent.ParseAdd("LearnPip");
                request.Headers.Accept.ParseAdd("application/vnd.github+json");
                using var response = await context.Backchannel.SendAsync(
                    request,
                    context.HttpContext.RequestAborted);
                response.EnsureSuccessStatusCode();
                using var payload = await JsonDocument.ParseAsync(
                    await response.Content.ReadAsStreamAsync(context.HttpContext.RequestAborted),
                    cancellationToken: context.HttpContext.RequestAborted);
                if (!payload.RootElement.TryGetProperty("id", out var id) ||
                    id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out var number) ||
                    number <= 0)
                {
                    throw new InvalidOperationException("GitHub did not return a stable user ID.");
                }

                context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, number.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)));
            };
            options.Events.OnTicketReceived = async context =>
            {
                context.HandleResponse();
                var subject = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                await ExternalLoginCompletion.CompleteAsync(
                    context.HttpContext,
                    context.Properties,
                    "https://github.com",
                    subject ?? string.Empty,
                    configuration["GithubOAuth:PostLoginPath"]);
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
