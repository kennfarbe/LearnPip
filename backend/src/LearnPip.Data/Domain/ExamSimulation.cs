// <copyright file="ExamSimulation.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell ExamSimulation.
/// </summary>
public sealed class ExamSimulation
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft profile version id ab oder legt den Wert fest.
    /// </summary>
    public Guid ProfileVersionId { get; set; }

    /// <summary>
    /// Ruft profile version ab oder legt den Wert fest.
    /// </summary>
    public ExamProfileVersion ProfileVersion { get; set; } = null!;

    /// <summary>
    /// Ruft snapshot json ab oder legt den Wert fest.
    /// </summary>
    public string SnapshotJson { get; set; } = string.Empty;

    /// <summary>
    /// Ruft answers json ab oder legt den Wert fest.
    /// </summary>
    public string AnswersJson { get; set; } = "{}";

    /// <summary>
    /// Ruft current part index ab oder legt den Wert fest.
    /// </summary>
    public int CurrentPartIndex { get; set; }

    /// <summary>
    /// Ruft started at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft part started at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset PartStartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft completed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Ruft result json ab oder legt den Wert fest.
    /// </summary>
    public string? ResultJson { get; set; }
}
