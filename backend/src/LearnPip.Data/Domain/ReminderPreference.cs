// <copyright file="ReminderPreference.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell ReminderPreference.
/// </summary>
public sealed class ReminderPreference
{
    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft enabled ab oder legt den Wert fest.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Ruft interval days ab oder legt den Wert fest.
    /// </summary>
    public int IntervalDays { get; set; } = 7;

    /// <summary>
    /// Ruft quiet start minute ab oder legt den Wert fest.
    /// </summary>
    public int QuietStartMinute { get; set; } = 22 * 60;

    /// <summary>
    /// Ruft quiet end minute ab oder legt den Wert fest.
    /// </summary>
    public int QuietEndMinute { get; set; } = 8 * 60;

    /// <summary>
    /// Ruft time zone id ab oder legt den Wert fest.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Berlin";

    /// <summary>
    /// Ruft last notified activity at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? LastNotifiedActivityAtUtc { get; set; }

    /// <summary>
    /// Ruft last sent at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? LastSentAtUtc { get; set; }
}
