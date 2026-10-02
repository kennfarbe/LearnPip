// <copyright file="StudyAttempt.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell StudyAttempt.
/// </summary>
public sealed class StudyAttempt
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt study session id.
    /// </summary>
    public Guid StudySessionId { get; set; }

    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt is correct.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Holt oder setzt was guessed.
    /// </summary>
    public bool WasGuessed { get; set; }

    /// <summary>
    /// Holt oder setzt explanation viewed at utc.
    /// </summary>
    public DateTimeOffset? ExplanationViewedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt answered at utc.
    /// </summary>
    public DateTimeOffset AnsweredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt study session.
    /// </summary>
    public StudySession StudySession { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt selections.
    /// </summary>
    public ICollection<StudyAttemptSelection> Selections { get; set; } = [];
}
