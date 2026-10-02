// <copyright file="FamilyGoal.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class FamilyGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FamilyLinkId { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTimeOffset? TargetAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
