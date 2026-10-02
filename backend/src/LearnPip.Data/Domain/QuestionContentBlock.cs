// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class QuestionContentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? QuestionVersionId { get; set; }

    public Guid? AnswerOptionId { get; set; }

    public string Section { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string? Text { get; set; }

    public Guid? MediaAssetId { get; set; }

    public QuestionVersion? QuestionVersion { get; set; }

    public AnswerOption? AnswerOption { get; set; }

    public MediaAsset? MediaAsset { get; set; }
}
