// <copyright file="OfficialCatalogEdition.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell OfficialCatalogEdition.
/// </summary>
public sealed class OfficialCatalogEdition
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
    /// Ruft revision ab oder legt den Wert fest.
    /// </summary>
    public string Revision { get; set; } = string.Empty;

    /// <summary>
    /// Ruft source url ab oder legt den Wert fest.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Ruft license ab oder legt den Wert fest.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Ruft attribution ab oder legt den Wert fest.
    /// </summary>
    public string Attribution { get; set; } = string.Empty;

    /// <summary>
    /// Ruft changed on ab oder legt den Wert fest.
    /// </summary>
    public DateOnly ChangedOn { get; set; }

    /// <summary>
    /// Ruft imported at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft questions json ab oder legt den Wert fest.
    /// </summary>
    public string QuestionsJson { get; set; } = string.Empty;
}
