// <copyright file="ExternalIdentity.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell ExternalIdentity.
/// </summary>
public sealed class ExternalIdentity
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
    /// Ruft provider ab oder legt den Wert fest.
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Ruft subject ab oder legt den Wert fest.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft account ab oder legt den Wert fest.
    /// </summary>
    public Account Account { get; set; } = null!;
}
