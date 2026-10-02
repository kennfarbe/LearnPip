// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class AccountInactivityWarning
{
    public Guid AccountId { get; set; }

    public int PhaseDays { get; set; }

    public DateTimeOffset ActivityAtUtc { get; set; }

    public DateTimeOffset ClaimedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAtUtc { get; set; }

    public string DeliveryStatus { get; set; } = "claimed";
}
