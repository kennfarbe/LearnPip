// <copyright file="QuestionObjective.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionObjective.
/// </summary>
public sealed class QuestionObjective
{
    /// <summary>
    /// Ruft question id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Ruft exam objective id ab oder legt den Wert fest.
    /// </summary>
    public Guid ExamObjectiveId { get; set; }

    /// <summary>
    /// Ruft question ab oder legt den Wert fest.
    /// </summary>
    public Question Question { get; set; } = null!;

    /// <summary>
    /// Ruft exam objective ab oder legt den Wert fest.
    /// </summary>
    public ExamObjective ExamObjective { get; set; } = null!;
}
