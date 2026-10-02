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
    /// Ruft key ab oder legt den Wert fest.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Ruft value ab oder legt den Wert fest.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Änderungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
