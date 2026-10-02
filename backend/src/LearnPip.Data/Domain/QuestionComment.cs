// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class QuestionComment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionVersionId { get; set; }

    public Guid AccountId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RemovedAtUtc { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = null!;
}
