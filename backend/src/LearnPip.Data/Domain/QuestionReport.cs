// <copyright file="QuestionReport.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionReport.
/// </summary>
public sealed class QuestionReport
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
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft reason ab oder legt den Wert fest.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Ruft details ab oder legt den Wert fest.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Bearbeitungsstatus ab oder legt den Wert fest.
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft closed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? ClosedAtUtc { get; set; }

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion QuestionVersion { get; set; } = null!;
}
