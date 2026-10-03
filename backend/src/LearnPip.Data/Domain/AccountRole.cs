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
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt role definition id.
    /// </summary>
    public Guid RoleDefinitionId { get; set; }

    /// <summary>
    /// Holt oder setzt granted at utc.
    /// </summary>
    public DateTimeOffset GrantedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt zugeordnetes Konto.
    /// </summary>
    public Account Account { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt role definition.
    /// </summary>
    public RoleDefinition RoleDefinition { get; set; } = null!;
}
