// <copyright file="ReminderPreference.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class ReminderPreference
{
    public Guid AccountId { get; set; }

    public bool Enabled { get; set; }

    public int IntervalDays { get; set; } = 7;

    public int QuietStartMinute { get; set; } = 22 * 60;

    public int QuietEndMinute { get; set; } = 8 * 60;

    public string TimeZoneId { get; set; } = "Europe/Berlin";

    public DateTimeOffset? LastNotifiedActivityAtUtc { get; set; }

    public DateTimeOffset? LastSentAtUtc { get; set; }
}
