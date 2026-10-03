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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt study group id.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Holt oder setzt code hash.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt created by account id.
    /// </summary>
    public Guid CreatedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt expires at utc.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt max uses.
    /// </summary>
    public int MaxUses { get; set; }

    /// <summary>
    /// Holt oder setzt used count.
    /// </summary>
    public int UsedCount { get; set; }

    /// <summary>
    /// Holt oder setzt revoked at utc.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt study group.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;
}
