// <copyright file="FamilyGoal.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell FamilyGoal.
/// </summary>
public sealed class FamilyGoal
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft family link id ab oder legt den Wert fest.
    /// </summary>
    public Guid FamilyLinkId { get; set; }

    /// <summary>
    /// Ruft Titel ab oder legt den Wert fest.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ruft target at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? TargetAtUtc { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
