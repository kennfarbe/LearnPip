// <copyright file="AccountRole.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class AccountRole
{
    public Guid AccountId { get; set; }

    public Guid RoleDefinitionId { get; set; }

    public DateTimeOffset GrantedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Account Account { get; set; } = null!;

    public RoleDefinition RoleDefinition { get; set; } = null!;
}
