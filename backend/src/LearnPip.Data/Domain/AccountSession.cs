// <copyright file="AccountSession.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class AccountSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public Account Account { get; set; } = null!;
}
