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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt started at utc.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt completed at utc.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt plan json.
    /// </summary>
    public string? PlanJson { get; set; }

    /// <summary>
    /// Holt oder setzt account.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt attempts.
    /// </summary>
    public ICollection<StudyAttempt> Attempts { get; set; } = [];
}
