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
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft phase days ab oder legt den Wert fest.
    /// </summary>
    public int PhaseDays { get; set; }

    /// <summary>
    /// Ruft activity at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ActivityAtUtc { get; set; }

    /// <summary>
    /// Ruft claimed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ClaimedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft sent at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? SentAtUtc { get; set; }

    /// <summary>
    /// Ruft delivery status ab oder legt den Wert fest.
    /// </summary>
    public string DeliveryStatus { get; set; } = "claimed";
}
