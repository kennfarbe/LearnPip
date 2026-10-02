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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft text ab oder legt den Wert fest.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Ruft is correct ab oder legt den Wert fest.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Ruft sort order ab oder legt den Wert fest.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;

    /// <summary>
    /// Ruft blocks ab oder legt den Wert fest.
    /// </summary>
    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];
}
