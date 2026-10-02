// <copyright file="FamilyLinkEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell FamilyLinkEvent.
/// </summary>
public sealed class FamilyLinkEvent
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
    /// Holt oder setzt actor account id.
    /// </summary>
    public Guid ActorAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
