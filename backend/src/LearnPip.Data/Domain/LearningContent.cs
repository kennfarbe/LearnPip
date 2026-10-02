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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Eigentümerkontos.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt Titel.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt questions.
    /// </summary>
    public ICollection<Question> Questions { get; set; } = [];
}
