// <copyright file="PublicSubmission.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell PublicSubmission.
/// </summary>
public sealed class PublicSubmission
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
    /// Holt oder setzt Bearbeitungsstatus.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Holt oder setzt license choice.
    /// </summary>
    public string LicenseChoice { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt author attribution.
    /// </summary>
    public string AuthorAttribution { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt age declaration.
    /// </summary>
    public string AgeDeclaration { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt guardian approved by account id.
    /// </summary>
    public Guid? GuardianApprovedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt guardian approved at utc.
    /// </summary>
    public DateTimeOffset? GuardianApprovedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt einen Wert, der angibt, ob die Nutzungsrechte bestätigt wurden.
    /// </summary>
    public bool RightsConfirmed { get; set; }

    /// <summary>
    /// Holt oder setzt einen Wert, der angibt, ob die Bildrechte bestätigt wurden.
    /// </summary>
    public bool ImageRightsConfirmed { get; set; }

    /// <summary>
    /// Holt oder setzt submitted at utc.
    /// </summary>
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt reviewed at utc.
    /// </summary>
    public DateTimeOffset? ReviewedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt reviewed by account id.
    /// </summary>
    public Guid? ReviewedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt review note.
    /// </summary>
    public string? ReviewNote { get; set; }

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
