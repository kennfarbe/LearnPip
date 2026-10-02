// <copyright file="StudyGroup.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell StudyGroup.
/// </summary>
public sealed class StudyGroup
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft Kennung des Eigentümerkontos ab oder legt den Wert fest.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Ruft Name ab oder legt den Wert fest.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft Löschzeitpunkt in UTC, sofern vorhanden ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Ruft memberships ab oder legt den Wert fest.
    /// </summary>
    public ICollection<GroupMembership> Memberships { get; set; } = [];
}
