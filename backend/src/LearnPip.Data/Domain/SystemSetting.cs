// <copyright file="SystemSetting.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell SystemSetting.
/// </summary>
public sealed class SystemSetting
{
    /// <summary>
    /// Holt oder setzt key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Änderungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
