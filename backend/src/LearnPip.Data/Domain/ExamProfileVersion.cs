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
    /// Holt oder setzt amateur class.
    /// </summary>
    public string AmateurClass { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Holt oder setzt catalog edition id.
    /// </summary>
    public Guid CatalogEditionId { get; set; }

    /// <summary>
    /// Holt oder setzt catalog edition.
    /// </summary>
    public OfficialCatalogEdition CatalogEdition { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt parts json.
    /// </summary>
    public string PartsJson { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt schedule json.
    /// </summary>
    public string ScheduleJson { get; set; } = "[]";

    /// <summary>
    /// Holt oder setzt rules source url.
    /// </summary>
    public string? RulesSourceUrl { get; set; }

    /// <summary>
    /// Holt oder setzt rules checked on.
    /// </summary>
    public DateOnly? RulesCheckedOn { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
