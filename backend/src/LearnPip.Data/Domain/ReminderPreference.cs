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
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt einen Wert, der angibt, ob die Erinnerung aktiviert ist.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Holt oder setzt interval days.
    /// </summary>
    public int IntervalDays { get; set; } = 7;

    /// <summary>
    /// Holt oder setzt quiet start minute.
    /// </summary>
    public int QuietStartMinute { get; set; } = 22 * 60;

    /// <summary>
    /// Holt oder setzt quiet end minute.
    /// </summary>
    public int QuietEndMinute { get; set; } = 8 * 60;

    /// <summary>
    /// Holt oder setzt time zone id.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Berlin";

    /// <summary>
    /// Holt oder setzt last notified activity at utc.
    /// </summary>
    public DateTimeOffset? LastNotifiedActivityAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt last sent at utc.
    /// </summary>
    public DateTimeOffset? LastSentAtUtc { get; set; }
}
