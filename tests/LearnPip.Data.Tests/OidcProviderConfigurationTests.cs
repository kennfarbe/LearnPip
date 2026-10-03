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
    private static readonly string[] NoProviders = [];
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
            ["Oidc:Providers:facebook:Authority"] = "https://other.example.invalid",
            ["Oidc:Providers:facebook:ClientId"] = "client",
            ["Oidc:Providers:facebook:ClientSecret"] = "secret",
            ["Oidc:Providers:unknown:Authority"] = "https://other.example.invalid",
            ["Oidc:Providers:unknown:ClientId"] = "client",
            ["Oidc:Providers:unknown:ClientSecret"] = "secret",
        });

        Assert.Equal(NoProviders, OidcSetup.EnabledProviders(configuration));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "apple"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "unknown"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "GitHub"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "github"));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "facebook"));
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
            ["Oidc:Providers:apple:Authority"] = "https://appleid.apple.com",
            ["Oidc:Providers:apple:ClientId"] = "apple",
            ["Oidc:Providers:apple:ClientSecret"] = "apple-secret",
            ["Oidc:Providers:microsoft:Authority"] = "https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/v2.0",
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

    /// <summary>
    /// Fehlerhafte Issuer-Adressen werden nicht als Anmeldeanbieter angeboten.
    /// </summary>
    /// <param name="authority">Die zu prüfende Issuer-Adresse.</param>
    [Theory]
    [InlineData("http://issuer.example.invalid")]
    [InlineData("not-a-url")]
    [InlineData("https://user:password@issuer.example.invalid")]
    [InlineData("https://issuer.example.invalid/?token=example")]
    [InlineData("https://issuer.example.invalid/#fragment")]
    public void InvalidIssuerDoesNotEnableProvider(string authority)
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Oidc:Providers:microsoft:Authority"] = authority,
            ["Oidc:Providers:microsoft:ClientId"] = "client",
            ["Oidc:Providers:microsoft:ClientSecret"] = "secret",
        });

        Assert.Empty(OidcSetup.EnabledProviders(configuration));
        Assert.Null(OidcSetup.ProviderScheme(configuration, "microsoft"));
    }

    /// <summary>
    /// Fremde Issuer dürfen nicht unter dem Namen eines bekannten Anbieters auftreten.
    /// </summary>
    /// <param name="provider">Der deklarierte Anbieter.</param>
    /// <param name="authority">Der zu prüfende Issuer.</param>
    [Theory]
    [InlineData("apple", "https://issuer.example.invalid")]
    [InlineData("apple", "https://appleid.apple.com.evil.invalid")]
    [InlineData("microsoft", "https://issuer.example.invalid")]
    [InlineData("microsoft", "https://login.microsoftonline.com/common/v2.0")]
    [InlineData("microsoft", "https://login.microsoftonline.com/organizations/v2.0")]
    public void ProviderAuthorityMustMatchDeclaredProvider(string provider, string authority)
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            [$"Oidc:Providers:{provider}:Authority"] = authority,
            [$"Oidc:Providers:{provider}:ClientId"] = "client",
            [$"Oidc:Providers:{provider}:ClientSecret"] = "secret",
        });

        Assert.Null(OidcSetup.ProviderScheme(configuration, provider));
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
