// <copyright file="FamilyLinkEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class FamilyLinkEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid FamilyLinkId { get; set; }

    public Guid ActorAccountId { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
