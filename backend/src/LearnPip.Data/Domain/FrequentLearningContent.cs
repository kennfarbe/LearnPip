// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class FrequentLearningContent
{
    public Guid AccountId { get; set; }

    public Guid LearningContentId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public LearningContent LearningContent { get; set; } = null!;
}
