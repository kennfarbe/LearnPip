// <copyright file="QuestionModerationEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class QuestionModerationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionVersionId { get; set; }

    public Guid ModeratorAccountId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public Guid? ReplacementVersionId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
