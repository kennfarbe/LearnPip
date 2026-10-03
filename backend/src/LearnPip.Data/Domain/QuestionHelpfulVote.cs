// <copyright file="QuestionHelpfulVote.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionHelpfulVote.
/// </summary>
public sealed class QuestionHelpfulVote
{
    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt einen Wert, der angibt, ob die Stimme den Inhalt als hilfreich bewertet.
    /// </summary>
    public bool Helpful { get; set; }

    /// <summary>
    /// Holt oder setzt Änderungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
