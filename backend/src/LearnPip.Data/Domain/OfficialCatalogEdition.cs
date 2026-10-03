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
    /// Holt oder setzt revision.
    /// </summary>
    public string Revision { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt source url.
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt license.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt attribution.
    /// </summary>
    public string Attribution { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt changed on.
    /// </summary>
    public DateOnly ChangedOn { get; set; }

    /// <summary>
    /// Holt oder setzt imported at utc.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt questions json.
    /// </summary>
    public string QuestionsJson { get; set; } = string.Empty;
}
