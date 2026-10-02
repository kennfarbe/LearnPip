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
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt day.
    /// </summary>
    public DateOnly Day { get; set; }

    /// <summary>
    /// Holt oder setzt mode.
    /// </summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt used requests.
    /// </summary>
    public int UsedRequests { get; set; }
}
