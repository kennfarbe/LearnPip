// <copyright file="GroupCatalogShare.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class GroupCatalogShare
{
    public Guid StudyGroupId { get; set; }

    public Guid PrivateCatalogId { get; set; }

    public Guid SharedByAccountId { get; set; }

    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public StudyGroup StudyGroup { get; set; } = null!;

    public PrivateCatalog PrivateCatalog { get; set; } = null!;
}
