// <copyright file="StudyAttemptSelection.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell StudyAttemptSelection.
/// </summary>
public sealed class StudyAttemptSelection
{
    /// <summary>
    /// Ruft study attempt id ab oder legt den Wert fest.
    /// </summary>
    public Guid StudyAttemptId { get; set; }

    /// <summary>
    /// Ruft answer option id ab oder legt den Wert fest.
    /// </summary>
    public Guid AnswerOptionId { get; set; }

    /// <summary>
    /// Ruft study attempt ab oder legt den Wert fest.
    /// </summary>
    public StudyAttempt StudyAttempt { get; set; } = null!;

    /// <summary>
    /// Ruft answer option ab oder legt den Wert fest.
    /// </summary>
    public AnswerOption AnswerOption { get; set; } = null!;
}
