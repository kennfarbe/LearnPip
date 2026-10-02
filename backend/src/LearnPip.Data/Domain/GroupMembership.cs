// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class GroupMembership
{
    public Guid StudyGroupId { get; set; }

    public Guid AccountId { get; set; }

    public Guid RoleDefinitionId { get; set; }

    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public StudyGroup StudyGroup { get; set; } = null!;

    public Account Account { get; set; } = null!;

    public RoleDefinition RoleDefinition { get; set; } = null!;
}
