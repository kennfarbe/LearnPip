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
    /// Ruft study group id ab oder legt den Wert fest.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft role definition id ab oder legt den Wert fest.
    /// </summary>
    public Guid RoleDefinitionId { get; set; }

    /// <summary>
    /// Ruft joined at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft study group ab oder legt den Wert fest.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;

    /// <summary>
    /// Ruft account ab oder legt den Wert fest.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Ruft role definition ab oder legt den Wert fest.
    /// </summary>
    public RoleDefinition RoleDefinition { get; set; } = null!;
}
