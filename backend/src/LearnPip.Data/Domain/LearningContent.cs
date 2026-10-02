// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class LearningContent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerAccountId { get; set; }

    public string Title { get; set; } = string.Empty;

    public ICollection<Question> Questions { get; set; } = [];
}
