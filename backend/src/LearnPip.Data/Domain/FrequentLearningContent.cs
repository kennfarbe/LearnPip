// <copyright file="FrequentLearningContent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell FrequentLearningContent.
/// </summary>
public sealed class FrequentLearningContent
{
    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft learning content id ab oder legt den Wert fest.
    /// </summary>
    public Guid LearningContentId { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft learning content ab oder legt den Wert fest.
    /// </summary>
    public LearningContent LearningContent { get; set; } = null!;
}
