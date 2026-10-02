// <copyright file="EmailLoginCode.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell EmailLoginCode.
/// </summary>
public sealed class EmailLoginCode
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft email ab oder legt den Wert fest.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Ruft purpose ab oder legt den Wert fest.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Ruft initiating session id ab oder legt den Wert fest.
    /// </summary>
    public Guid? InitiatingSessionId { get; set; }

    /// <summary>
    /// Ruft code hash ab oder legt den Wert fest.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft expires at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Ruft consumed at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? ConsumedAtUtc { get; set; }

    /// <summary>
    /// Ruft failed attempts ab oder legt den Wert fest.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Ruft zugeordnetes Konto ab oder legt den Wert fest.
    /// </summary>
    public Account? Account { get; set; }

    /// <summary>
    /// Ruft initiating session ab oder legt den Wert fest.
    /// </summary>
    public AccountSession? InitiatingSession { get; set; }
}
