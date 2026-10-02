// <copyright file="GroupInvitation.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class GroupInvitation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudyGroupId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public Guid CreatedByAccountId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public int MaxUses { get; set; }

    public int UsedCount { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public StudyGroup StudyGroup { get; set; } = null!;
}
