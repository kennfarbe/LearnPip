// <copyright file="RoleDefinition.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class RoleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Scope { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public ICollection<AccountRole> AccountRoles { get; set; } = [];
}
