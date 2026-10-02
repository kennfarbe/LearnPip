// <copyright file="PublicSubmission.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class PublicSubmission
{
    public Guid QuestionVersionId { get; set; }

    public Guid AccountId { get; set; }

    public string Status { get; set; } = "pending";

    public string LicenseChoice { get; set; } = string.Empty;

    public string AuthorAttribution { get; set; } = string.Empty;

    public string AgeDeclaration { get; set; } = string.Empty;

    public Guid? GuardianApprovedByAccountId { get; set; }

    public DateTimeOffset? GuardianApprovedAtUtc { get; set; }

    public bool RightsConfirmed { get; set; }

    public bool ImageRightsConfirmed { get; set; }

    public DateTimeOffset SubmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    public Guid? ReviewedByAccountId { get; set; }

    public string? ReviewNote { get; set; }

    public QuestionVersion QuestionVersion { get; set; } = null!;
}
