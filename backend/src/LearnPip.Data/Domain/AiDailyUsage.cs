// <copyright file="AiDailyUsage.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AiDailyUsage.
/// </summary>
public sealed class AiDailyUsage
{
    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft day ab oder legt den Wert fest.
    /// </summary>
    public DateOnly Day { get; set; }

    /// <summary>
    /// Ruft mode ab oder legt den Wert fest.
    /// </summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// Ruft used requests ab oder legt den Wert fest.
    /// </summary>
    public int UsedRequests { get; set; }
}
