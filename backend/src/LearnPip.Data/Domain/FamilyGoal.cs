// <copyright file="FamilyGoal.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell FamilyGoal.
/// </summary>
public sealed class FamilyGoal
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt family link id.
    /// </summary>
    public Guid FamilyLinkId { get; set; }

    /// <summary>
    /// Holt oder setzt Titel.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt target at utc.
    /// </summary>
    public DateTimeOffset? TargetAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
