// <copyright file="AdministrationAuditEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AdministrationAuditEvent.
/// </summary>
public sealed class AdministrationAuditEvent
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft actor account id ab oder legt den Wert fest.
    /// </summary>
    public Guid? ActorAccountId { get; set; }

    /// <summary>
    /// Ruft action ab oder legt den Wert fest.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Ruft target ab oder legt den Wert fest.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Ruft previous value ab oder legt den Wert fest.
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// Ruft new value ab oder legt den Wert fest.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
