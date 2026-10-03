// <copyright file="QuestionContentBlock.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionContentBlock.
/// </summary>
public sealed class QuestionContentBlock
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid? QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt answer option id.
    /// </summary>
    public Guid? AnswerOptionId { get; set; }

    /// <summary>
    /// Holt oder setzt section.
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt sort order.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Holt oder setzt kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt text.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Holt oder setzt media asset id.
    /// </summary>
    public Guid? MediaAssetId { get; set; }

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion? QuestionVersion { get; set; }

    /// <summary>
    /// Holt oder setzt answer option.
    /// </summary>
    public AnswerOption? AnswerOption { get; set; }

    /// <summary>
    /// Holt oder setzt media asset.
    /// </summary>
    public MediaAsset? MediaAsset { get; set; }
}
