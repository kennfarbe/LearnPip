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
    /// Holt oder setzt study attempt id.
    /// </summary>
    public Guid StudyAttemptId { get; set; }

    /// <summary>
    /// Holt oder setzt answer option id.
    /// </summary>
    public Guid AnswerOptionId { get; set; }

    /// <summary>
    /// Holt oder setzt study attempt.
    /// </summary>
    public StudyAttempt StudyAttempt { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt answer option.
    /// </summary>
    public AnswerOption AnswerOption { get; set; } = null!;
}
