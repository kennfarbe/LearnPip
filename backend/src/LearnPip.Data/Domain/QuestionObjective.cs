// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class QuestionObjective
{
    public Guid QuestionId { get; set; }

    public Guid ExamObjectiveId { get; set; }

    public Question Question { get; set; } = null!;

    public ExamObjective ExamObjective { get; set; } = null!;
}
