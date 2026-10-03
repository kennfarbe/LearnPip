// <copyright file="QuestionTranslation.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionTranslation.
/// </summary>
public sealed class QuestionTranslation
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt language.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt revision.
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Holt oder setzt Bearbeitungsstatus.
    /// </summary>
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Holt oder setzt payload json.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt license.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt provenance.
    /// </summary>
    public string Provenance { get; set; } = "manual";

    /// <summary>
    /// Holt oder setzt created by account id.
    /// </summary>
    public Guid CreatedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt approved at utc.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
