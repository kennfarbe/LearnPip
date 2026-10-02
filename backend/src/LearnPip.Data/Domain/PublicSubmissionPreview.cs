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
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft token hash ab oder legt den Wert fest.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Ruft expires at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
