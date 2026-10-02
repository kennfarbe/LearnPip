// <copyright file="QuestionTranslation.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class QuestionTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionVersionId { get; set; }

    public string Language { get; set; } = string.Empty;

    public int Revision { get; set; }

    public string Status { get; set; } = "draft";

    public string PayloadJson { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string Provenance { get; set; } = "manual";

    public Guid CreatedByAccountId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = null!;
}
