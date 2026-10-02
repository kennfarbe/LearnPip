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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft fachlicher Code ab oder legt den Wert fest.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Titel ab oder legt den Wert fest.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Beschreibung ab oder legt den Wert fest.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Ruft question objectives ab oder legt den Wert fest.
    /// </summary>
    public ICollection<QuestionObjective> QuestionObjectives { get; set; } = [];
}
