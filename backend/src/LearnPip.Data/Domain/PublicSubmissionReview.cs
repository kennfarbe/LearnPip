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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft moderator account id ab oder legt den Wert fest.
    /// </summary>
    public Guid ModeratorAccountId { get; set; }

    /// <summary>
    /// Ruft decision ab oder legt den Wert fest.
    /// </summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// Ruft correctness checked ab oder legt den Wert fest.
    /// </summary>
    public bool CorrectnessChecked { get; set; }

    /// <summary>
    /// Ruft image rights checked ab oder legt den Wert fest.
    /// </summary>
    public bool ImageRightsChecked { get; set; }

    /// <summary>
    /// Ruft personal data checked ab oder legt den Wert fest.
    /// </summary>
    public bool PersonalDataChecked { get; set; }

    /// <summary>
    /// Ruft duplicate checked ab oder legt den Wert fest.
    /// </summary>
    public bool DuplicateChecked { get; set; }

    /// <summary>
    /// Ruft note ab oder legt den Wert fest.
    /// </summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
