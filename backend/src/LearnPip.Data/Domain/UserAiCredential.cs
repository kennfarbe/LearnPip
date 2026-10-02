// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class UserAiCredential
{
    public Guid AccountId { get; set; }

    public string Ciphertext { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
