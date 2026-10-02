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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt profile version id.
    /// </summary>
    public Guid ProfileVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt profile version.
    /// </summary>
    public ExamProfileVersion ProfileVersion { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt snapshot json.
    /// </summary>
    public string SnapshotJson { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt answers json.
    /// </summary>
    public string AnswersJson { get; set; } = "{}";

    /// <summary>
    /// Holt oder setzt current part index.
    /// </summary>
    public int CurrentPartIndex { get; set; }

    /// <summary>
    /// Holt oder setzt started at utc.
    /// </summary>
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt part started at utc.
    /// </summary>
    public DateTimeOffset PartStartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt completed at utc.
    /// </summary>
    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt result json.
    /// </summary>
    public string? ResultJson { get; set; }
}
