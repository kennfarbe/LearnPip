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
    /// Holt oder setzt question id.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Holt oder setzt payload json.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Änderungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt question.
    /// </summary>
    public Question Question { get; set; } = null!;
}
