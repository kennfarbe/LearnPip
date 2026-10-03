// <copyright file="QuestionReport.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionReport.
/// </summary>
public sealed class QuestionReport
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt details.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Bearbeitungsstatus.
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt closed at utc.
    /// </summary>
    public DateTimeOffset? ClosedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
