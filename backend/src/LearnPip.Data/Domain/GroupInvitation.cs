// <copyright file="GroupInvitation.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell GroupInvitation.
/// </summary>
public sealed class GroupInvitation
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft study group id ab oder legt den Wert fest.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Ruft code hash ab oder legt den Wert fest.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Ruft created by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid CreatedByAccountId { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft expires at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Ruft max uses ab oder legt den Wert fest.
    /// </summary>
    public int MaxUses { get; set; }

    /// <summary>
    /// Ruft used count ab oder legt den Wert fest.
    /// </summary>
    public int UsedCount { get; set; }

    /// <summary>
    /// Ruft revoked at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Ruft study group ab oder legt den Wert fest.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;
}
