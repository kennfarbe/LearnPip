// <copyright file="TranslationReport.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class TranslationReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionTranslationId { get; set; }

    public Guid AccountId { get; set; }

    public string Details { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public QuestionTranslation Translation { get; set; } = null!;
}
