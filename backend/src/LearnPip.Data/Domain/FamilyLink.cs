// <copyright file="FamilyLink.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell FamilyLink.
/// </summary>
public sealed class FamilyLink
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft child account id ab oder legt den Wert fest.
    /// </summary>
    public Guid ChildAccountId { get; set; }

    /// <summary>
    /// Ruft parent account id ab oder legt den Wert fest.
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// Ruft invite hash ab oder legt den Wert fest.
    /// </summary>
    public string InviteHash { get; set; } = string.Empty;

    /// <summary>
    /// Ruft invite expires at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset InviteExpiresAtUtc { get; set; }

    /// <summary>
    /// Ruft Bearbeitungsstatus ab oder legt den Wert fest.
    /// </summary>
    public string Status { get; set; } = "invited";

    /// <summary>
    /// Ruft verified by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid? VerifiedByAccountId { get; set; }

    /// <summary>
    /// Ruft verification reference ab oder legt den Wert fest.
    /// </summary>
    public string? VerificationReference { get; set; }

    /// <summary>
    /// Ruft verified at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Ruft activated at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? ActivatedAtUtc { get; set; }

    /// <summary>
    /// Ruft revoked at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Ruft revoked by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid? RevokedByAccountId { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
