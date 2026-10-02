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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid? QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft answer option id ab oder legt den Wert fest.
    /// </summary>
    public Guid? AnswerOptionId { get; set; }

    /// <summary>
    /// Ruft section ab oder legt den Wert fest.
    /// </summary>
    public string Section { get; set; } = string.Empty;

    /// <summary>
    /// Ruft sort order ab oder legt den Wert fest.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Ruft kind ab oder legt den Wert fest.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Ruft text ab oder legt den Wert fest.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Ruft media asset id ab oder legt den Wert fest.
    /// </summary>
    public Guid? MediaAssetId { get; set; }

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion? QuestionVersion { get; set; }

    /// <summary>
    /// Ruft answer option ab oder legt den Wert fest.
    /// </summary>
    public AnswerOption? AnswerOption { get; set; }

    /// <summary>
    /// Ruft media asset ab oder legt den Wert fest.
    /// </summary>
    public MediaAsset? MediaAsset { get; set; }
}
