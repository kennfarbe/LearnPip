// <copyright file="QuestionDraft.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class QuestionDraft
{
    public Guid QuestionId { get; set; }

    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Question Question { get; set; } = null!;
}
