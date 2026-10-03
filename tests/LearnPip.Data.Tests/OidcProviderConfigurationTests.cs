// <copyright file="OidcProviderConfigurationTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Identity;
using Microsoft.Extensions.Configuration;

namespace LearnPip.Data.Tests;

/// <summary>
/// Prüft die getrennte und ausschließlich optionale OIDC-Anbieterkonfiguration.
/// </summary>
public sealed class OidcProviderConfigurationTests
{
    private static readonly string[] GithubOnly = ["github"];
    private static readonly string[] AppleAndMicrosoft = ["apple", "microsoft"];

    /// <summary>
    /// Unvollständige und unbekannte Anbieter bleiben deaktiviert.
    /// </summary>
    [Fact]
    public void ProvidersRequireCompleteExplicitConfiguration()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Oidc:Providers:apple:Authority"] = "https://issuer.example.invalid",
            ["Oidc:Providers:apple:ClientId"] = "client",
            ["Oidc:Providers:github:Authority"] = "https://other.example.invalid",
            ["Oidc:Providers:github:ClientId"] = "client",
            ["Oidc:Providers:github:ClientSecret"] = "secret",
            ["Oidc:Providers:unknown:Authority"] = "https://other.example.invalid",
            ["Oidc:Providers:unknown:ClientId"] = "client",
            ["Oidc:Providers:unknown:ClientSecret"] = "secret",
        });

        Assert.Equal(GithubOnly, OidcSetup.EnabledProviders(configuration));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "apple"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "unknown"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "GitHub"));
        Assert.Equal("LearnPipOidc-github", OidcSetup.ProviderScheme(configuration, "github"));
        Assert.False(OidcSetup.IsEnabled(configuration));
    }

    /// <summary>
    /// Mehrere konfigurierte Anbieter bleiben voneinander getrennt; Legacy bleibt separat.
    /// </summary>
    [Fact]
    public void MultipleProvidersAndLegacyConfigurationRemainIndependent()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Oidc:Authority"] = "https://legacy.example.invalid",
            ["Oidc:ClientId"] = "legacy",
            ["Oidc:ClientSecret"] = "legacy-secret",
            ["Oidc:Providers:apple:Authority"] = "https://apple.example.invalid",
            ["Oidc:Providers:apple:ClientId"] = "apple",
            ["Oidc:Providers:apple:ClientSecret"] = "apple-secret",
            ["Oidc:Providers:microsoft:Authority"] = "https://microsoft.example.invalid",
            ["Oidc:Providers:microsoft:ClientId"] = "microsoft",
            ["Oidc:Providers:microsoft:ClientSecret"] = "microsoft-secret",
        });

        Assert.True(OidcSetup.IsEnabled(configuration));
        Assert.Equal(AppleAndMicrosoft, OidcSetup.EnabledProviders(configuration));
        Assert.NotEqual(
            OidcSetup.ProviderScheme(configuration, "apple"),
            OidcSetup.ProviderScheme(configuration, "microsoft"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "facebook"));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
