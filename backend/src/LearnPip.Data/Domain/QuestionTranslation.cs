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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft language ab oder legt den Wert fest.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Ruft revision ab oder legt den Wert fest.
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Ruft Bearbeitungsstatus ab oder legt den Wert fest.
    /// </summary>
    public string Status { get; set; } = "draft";

    /// <summary>
    /// Ruft payload json ab oder legt den Wert fest.
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// Ruft source ab oder legt den Wert fest.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Ruft license ab oder legt den Wert fest.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Ruft provenance ab oder legt den Wert fest.
    /// </summary>
    public string Provenance { get; set; } = "manual";

    /// <summary>
    /// Ruft created by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid CreatedByAccountId { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft approved at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
