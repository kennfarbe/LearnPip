// <copyright file="PublicSubmissionPreview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell PublicSubmissionPreview.
/// </summary>
public sealed class PublicSubmissionPreview
{
    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt token hash.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt expires at utc.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
