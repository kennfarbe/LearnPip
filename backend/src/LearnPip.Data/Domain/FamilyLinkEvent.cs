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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft family link id ab oder legt den Wert fest.
    /// </summary>
    public Guid FamilyLinkId { get; set; }

    /// <summary>
    /// Ruft actor account id ab oder legt den Wert fest.
    /// </summary>
    public Guid ActorAccountId { get; set; }

    /// <summary>
    /// Ruft action ab oder legt den Wert fest.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
