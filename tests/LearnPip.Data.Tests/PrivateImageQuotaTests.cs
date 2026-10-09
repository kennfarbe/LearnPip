// <copyright file="PrivateImageQuotaTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Media;

namespace LearnPip.Data.Tests;

/// <summary>Prüft unabhängige Anzahl- und Speichergrenzen für vollständige Bildimporte.</summary>
public sealed class PrivateImageQuotaTests
{
    /// <summary>Ein großer Katalog ist möglich; beide harten Grenzen bleiben wirksam.</summary>
    /// <param name="count">Bildanzahl.</param>
    /// <param name="bytes">Gesamtbytes.</param>
    /// <param name="expected">Erwartete Zulässigkeit.</param>
    [Theory]
    [InlineData(687, 20 * 1024 * 1024, true)]
    [InlineData(2000, 100 * 1024 * 1024, true)]
    [InlineData(2001, 1, false)]
    [InlineData(1, (100 * 1024 * 1024) + 1, false)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    public void CountAndBytesRemainBounded(int count, long bytes, bool expected)
    {
        Assert.Equal(expected, PrivateImageQuota.Fits(count, bytes));
    }
}
