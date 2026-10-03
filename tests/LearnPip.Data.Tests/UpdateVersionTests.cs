// <copyright file="UpdateVersionTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Administration;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für den Versionsvergleich.
/// </summary>
public sealed class UpdateVersionTests
{
    /// <summary>
    /// Prüft den semantischen Vergleich stabiler Versionsnummern.
    /// </summary>
    /// <param name="left">Die erste zu vergleichende Versionsnummer.</param>
    /// <param name="right">Die zweite zu vergleichende Versionsnummer.</param>
    /// <param name="expected">Das erwartete Prüfergebnis.</param>
    [Theory]
    [InlineData("v1.3.1", "v1.3.1", 0)]
    [InlineData("v1.4.0", "v1.3.1", 1)]
    [InlineData("v1.2.9", "v1.3.1", -1)]
    [InlineData("v2.0.0", "v1.99.99", 1)]
    public void Stable_versions_are_compared_semantically(
        string left,
        string right,
        int expected)
    {
        Assert.Equal(expected, Math.Sign(UpdateService.Compare(left, right)));
    }

    /// <summary>
    /// Prüft den Ausschluss instabiler Versionsangaben vom Updatevergleich.
    /// </summary>
    /// <param name="value">Die zu prüfende instabile Versionsangabe.</param>
    [Theory]
    [InlineData("v1.4.0-beta.1")]
    [InlineData("main")]
    [InlineData("latest")]
    [InlineData("1.4.0")]
    public void Non_stable_versions_are_never_treated_as_newer(string value)
    {
        Assert.Equal(0, UpdateService.Compare(value, "v1.3.1"));
    }
}
