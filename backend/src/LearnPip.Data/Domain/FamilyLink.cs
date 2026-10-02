// <copyright file="FamilyLink.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class FamilyLink
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChildAccountId { get; set; }

    public Guid? ParentAccountId { get; set; }

    public string InviteHash { get; set; } = string.Empty;

    public DateTimeOffset InviteExpiresAtUtc { get; set; }

    public string Status { get; set; } = "invited";

    public Guid? VerifiedByAccountId { get; set; }

    public string? VerificationReference { get; set; }

    public DateTimeOffset? VerifiedAtUtc { get; set; }

    public DateTimeOffset? ActivatedAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public Guid? RevokedByAccountId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
