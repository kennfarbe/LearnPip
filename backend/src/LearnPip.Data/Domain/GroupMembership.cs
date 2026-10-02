// <copyright file="GroupMembership.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell GroupMembership.
/// </summary>
public sealed class GroupMembership
{
    /// <summary>
    /// Holt oder setzt study group id.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt role definition id.
    /// </summary>
    public Guid RoleDefinitionId { get; set; }

    /// <summary>
    /// Holt oder setzt joined at utc.
    /// </summary>
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt study group.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt account.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt role definition.
    /// </summary>
    public RoleDefinition RoleDefinition { get; set; } = null!;
}
