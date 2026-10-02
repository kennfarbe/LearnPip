// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class AnswerOption
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionVersionId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int SortOrder { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = null!;

    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];
}
