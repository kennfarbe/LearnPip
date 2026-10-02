// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? DisplayName { get; set; }

    public string AgeBand { get; set; } = "unknown";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public DateTimeOffset LastActivityAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DisabledAtUtc { get; set; }

    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];

    public ICollection<Question> Questions { get; set; } = [];

    public ICollection<StudySession> StudySessions { get; set; } = [];
}
