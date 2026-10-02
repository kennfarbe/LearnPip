// <copyright file="QuestionHelpfulVote.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class QuestionHelpfulVote
{
    public Guid QuestionVersionId { get; set; }

    public Guid AccountId { get; set; }

    public bool Helpful { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public QuestionVersion QuestionVersion { get; set; } = null!;
}
