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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Eigentümerkontos.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt Name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt Löschzeitpunkt in UTC, sofern vorhanden.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt memberships.
    /// </summary>
    public ICollection<GroupMembership> Memberships { get; set; } = [];
}
