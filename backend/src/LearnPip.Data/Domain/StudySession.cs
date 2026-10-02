// <copyright file="StudySession.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell StudySession.
/// </summary>
public sealed class StudySession
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft started at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft completed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Ruft plan json ab oder legt den Wert fest.
    /// </summary>
    public string? PlanJson { get; set; }

    /// <summary>
    /// Ruft account ab oder legt den Wert fest.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Ruft attempts ab oder legt den Wert fest.
    /// </summary>
    public ICollection<StudyAttempt> Attempts { get; set; } = [];
}
