// <copyright file="AccountRole.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AccountRole.
/// </summary>
public sealed class AccountRole
{
    /// <summary>
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft role definition id ab oder legt den Wert fest.
    /// </summary>
    public Guid RoleDefinitionId { get; set; }

    /// <summary>
    /// Ruft granted at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset GrantedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft zugeordnetes Konto ab oder legt den Wert fest.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Ruft role definition ab oder legt den Wert fest.
    /// </summary>
    public RoleDefinition RoleDefinition { get; set; } = null!;
}
