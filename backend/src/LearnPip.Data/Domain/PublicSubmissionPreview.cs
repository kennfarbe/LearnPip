// <copyright file="PublicSubmissionPreview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class PublicSubmissionPreview
{
    public Guid QuestionVersionId { get; set; }

    public Guid AccountId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}
