// <copyright file="RoleDefinition.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell RoleDefinition.
/// </summary>
public sealed class RoleDefinition
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft scope ab oder legt den Wert fest.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Ruft fachlicher Code ab oder legt den Wert fest.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Ruft Name ab oder legt den Wert fest.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Ruft account roles ab oder legt den Wert fest.
    /// </summary>
    public ICollection<AccountRole> AccountRoles { get; set; } = [];
}
