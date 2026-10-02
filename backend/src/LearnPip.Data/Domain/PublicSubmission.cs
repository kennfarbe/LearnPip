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
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft Bearbeitungsstatus ab oder legt den Wert fest.
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Ruft license choice ab oder legt den Wert fest.
    /// </summary>
    public string LicenseChoice { get; set; } = string.Empty;

    /// <summary>
    /// Ruft author attribution ab oder legt den Wert fest.
    /// </summary>
    public string AuthorAttribution { get; set; } = string.Empty;

    /// <summary>
    /// Ruft age declaration ab oder legt den Wert fest.
    /// </summary>
    public string AgeDeclaration { get; set; } = string.Empty;

    /// <summary>
    /// Ruft guardian approved by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid? GuardianApprovedByAccountId { get; set; }

    /// <summary>
    /// Ruft guardian approved at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? GuardianApprovedAtUtc { get; set; }

    /// <summary>
    /// Ruft rights confirmed ab oder legt den Wert fest.
    /// </summary>
    public bool RightsConfirmed { get; set; }

    /// <summary>
    /// Ruft image rights confirmed ab oder legt den Wert fest.
    /// </summary>
    public bool ImageRightsConfirmed { get; set; }

    /// <summary>
    /// Ruft submitted at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft reviewed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? ReviewedAtUtc { get; set; }

    /// <summary>
    /// Ruft reviewed by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid? ReviewedByAccountId { get; set; }

    /// <summary>
    /// Ruft review note ab oder legt den Wert fest.
    /// </summary>
    public string? ReviewNote { get; set; }

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
