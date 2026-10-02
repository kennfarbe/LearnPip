// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class StudyAttemptSelection
{
    public Guid StudyAttemptId { get; set; }

    public Guid AnswerOptionId { get; set; }

    public StudyAttempt StudyAttempt { get; set; } = null!;

    public AnswerOption AnswerOption { get; set; } = null!;
}
