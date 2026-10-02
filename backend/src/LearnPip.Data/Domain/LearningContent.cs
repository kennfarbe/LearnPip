// <copyright file="LearningContent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell LearningContent.
/// </summary>
public sealed class LearningContent
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft Kennung des Eigentümerkontos ab oder legt den Wert fest.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Ruft Titel ab oder legt den Wert fest.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ruft questions ab oder legt den Wert fest.
    /// </summary>
    public ICollection<Question> Questions { get; set; } = [];
}
