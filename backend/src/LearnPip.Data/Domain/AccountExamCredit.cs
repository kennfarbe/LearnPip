// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class AccountExamCredit
{
    public Guid AccountId { get; set; }

    public string Code { get; set; } = string.Empty;

    public DateTimeOffset ReportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
