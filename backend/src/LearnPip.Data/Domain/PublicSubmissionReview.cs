// <copyright file="PublicSubmissionReview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell PublicSubmissionReview.
/// </summary>
public sealed class PublicSubmissionReview
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
    /// Holt oder setzt moderator account id.
    /// </summary>
    public Guid ModeratorAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt decision.
    /// </summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt correctness checked.
    /// </summary>
    public bool CorrectnessChecked { get; set; }

    /// <summary>
    /// Holt oder setzt image rights checked.
    /// </summary>
    public bool ImageRightsChecked { get; set; }

    /// <summary>
    /// Holt oder setzt personal data checked.
    /// </summary>
    public bool PersonalDataChecked { get; set; }

    /// <summary>
    /// Holt oder setzt duplicate checked.
    /// </summary>
    public bool DuplicateChecked { get; set; }

    /// <summary>
    /// Holt oder setzt note.
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
