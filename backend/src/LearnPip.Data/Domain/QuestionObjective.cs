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
    /// Holt oder setzt question id.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Holt oder setzt exam objective id.
    /// </summary>
    public Guid ExamObjectiveId { get; set; }

    /// <summary>
    /// Holt oder setzt question.
    /// </summary>
    public Question Question { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt exam objective.
    /// </summary>
    public ExamObjective ExamObjective { get; set; } = null!;
}
