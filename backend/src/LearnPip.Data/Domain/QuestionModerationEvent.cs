// <copyright file="QuestionModerationEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionModerationEvent.
/// </summary>
public sealed class QuestionModerationEvent
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
    /// Holt oder setzt moderator account id.
    /// </summary>
    public Guid ModeratorAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt note.
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt replacement version id.
    /// </summary>
    public Guid? ReplacementVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
