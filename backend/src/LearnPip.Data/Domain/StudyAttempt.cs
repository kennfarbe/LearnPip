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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft study session id ab oder legt den Wert fest.
    /// </summary>
    public Guid StudySessionId { get; set; }

    /// <summary>
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft is correct ab oder legt den Wert fest.
    /// </summary>
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Ruft was guessed ab oder legt den Wert fest.
    /// </summary>
    public bool WasGuessed { get; set; }

    /// <summary>
    /// Ruft explanation viewed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? ExplanationViewedAtUtc { get; set; }

    /// <summary>
    /// Ruft answered at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset AnsweredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft study session ab oder legt den Wert fest.
    /// </summary>
    public StudySession StudySession { get; set; } = null!;

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;

    /// <summary>
    /// Ruft selections ab oder legt den Wert fest.
    /// </summary>
    public ICollection<StudyAttemptSelection> Selections { get; set; } = [];
}
