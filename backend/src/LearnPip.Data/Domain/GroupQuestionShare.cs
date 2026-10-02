// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class GroupQuestionShare
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudyGroupId { get; set; }

    public Guid QuestionId { get; set; }

    public Guid SharedByAccountId { get; set; }

    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public StudyGroup StudyGroup { get; set; } = null!;

    public Question Question { get; set; } = null!;
}
