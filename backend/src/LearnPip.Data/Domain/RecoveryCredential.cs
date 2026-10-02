// <copyright file="RecoveryCredential.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class RecoveryCredential
{
    public Guid AccountId { get; set; }

    public string SecretHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Account Account { get; set; } = null!;
}
