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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt scope.
    /// </summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt fachlicher Code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt account roles.
    /// </summary>
    public ICollection<AccountRole> AccountRoles { get; set; } = [];
}
