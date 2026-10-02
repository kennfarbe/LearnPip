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
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft helpful ab oder legt den Wert fest.
    /// </summary>
    public bool Helpful { get; set; }

    /// <summary>
    /// Ruft Änderungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
