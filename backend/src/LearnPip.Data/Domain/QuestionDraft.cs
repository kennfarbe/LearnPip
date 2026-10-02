// <copyright file="QuestionDraft.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionDraft.
/// </summary>
public sealed class QuestionDraft
{
    /// <summary>
    /// Ruft question id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Ruft payload json ab oder legt den Wert fest.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Änderungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft question ab oder legt den Wert fest.
    /// </summary>
    public Question Question { get; set; } = null!;
}
