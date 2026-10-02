// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class QuestionReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionVersionId { get; set; }

    public Guid AccountId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public string Status { get; set; } = "open";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ClosedAtUtc { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = null!;
}
