// <copyright file="AnswerOption.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AnswerOption.
/// </summary>
public sealed class AnswerOption
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt is correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Holt oder setzt sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt blocks.
    /// </summary>
    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];
}
