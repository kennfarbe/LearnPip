// <copyright file="Account.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell Account.
/// </summary>
public sealed class Account
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft display name ab oder legt den Wert fest.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Ruft age band ab oder legt den Wert fest.
    /// </summary>
    public string AgeBand { get; set; } = "unknown";

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft Änderungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft Löschzeitpunkt in UTC, sofern vorhanden ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Ruft last activity at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset LastActivityAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft disabled at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? DisabledAtUtc { get; set; }

    /// <summary>
    /// Ruft external identities ab oder legt den Wert fest.
    /// </summary>
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];

    /// <summary>
    /// Ruft questions ab oder legt den Wert fest.
    /// </summary>
    public ICollection<Question> Questions { get; set; } = [];

    /// <summary>
    /// Ruft study sessions ab oder legt den Wert fest.
    /// </summary>
    public ICollection<StudySession> StudySessions { get; set; } = [];
}
