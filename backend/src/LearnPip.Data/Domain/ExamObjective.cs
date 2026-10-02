// <copyright file="ExamObjective.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell ExamObjective.
/// </summary>
public sealed class ExamObjective
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt fachlicher Code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Titel.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Beschreibung.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Holt oder setzt question objectives.
    /// </summary>
    public ICollection<QuestionObjective> QuestionObjectives { get; set; } = [];
}
