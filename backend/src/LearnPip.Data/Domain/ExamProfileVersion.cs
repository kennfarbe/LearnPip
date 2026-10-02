// <copyright file="ExamProfileVersion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell ExamProfileVersion.
/// </summary>
public sealed class ExamProfileVersion
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
    /// Ruft amateur class ab oder legt den Wert fest.
    /// </summary>
    public string AmateurClass { get; set; } = string.Empty;

    /// <summary>
    /// Ruft version ab oder legt den Wert fest.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Ruft catalog edition id ab oder legt den Wert fest.
    /// </summary>
    public Guid CatalogEditionId { get; set; }

    /// <summary>
    /// Ruft catalog edition ab oder legt den Wert fest.
    /// </summary>
    public OfficialCatalogEdition CatalogEdition { get; set; } = null!;

    /// <summary>
    /// Ruft parts json ab oder legt den Wert fest.
    /// </summary>
    public string PartsJson { get; set; } = string.Empty;

    /// <summary>
    /// Ruft schedule json ab oder legt den Wert fest.
    /// </summary>
    public string ScheduleJson { get; set; } = "[]";

    /// <summary>
    /// Ruft rules source url ab oder legt den Wert fest.
    /// </summary>
    public string? RulesSourceUrl { get; set; }

    /// <summary>
    /// Ruft rules checked on ab oder legt den Wert fest.
    /// </summary>
    public DateOnly? RulesCheckedOn { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
