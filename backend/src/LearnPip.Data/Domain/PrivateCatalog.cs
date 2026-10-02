// <copyright file="PrivateCatalog.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class PrivateCatalog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerAccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Question> Questions { get; set; } = [];
}
