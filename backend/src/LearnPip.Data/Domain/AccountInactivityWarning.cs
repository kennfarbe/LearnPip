// <copyright file="AccountInactivityWarning.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AccountInactivityWarning.
/// </summary>
public sealed class AccountInactivityWarning
{
    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt phase days.
    /// </summary>
    public int PhaseDays { get; set; }

    /// <summary>
    /// Holt oder setzt activity at utc.
    /// </summary>
    public DateTimeOffset ActivityAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt claimed at utc.
    /// </summary>
    public DateTimeOffset ClaimedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt sent at utc.
    /// </summary>
    public DateTimeOffset? SentAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt delivery status.
    /// </summary>
    public string DeliveryStatus { get; set; } = "claimed";
}
