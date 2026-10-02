// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class PublicSubmissionReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionVersionId { get; set; }

    public Guid ModeratorAccountId { get; set; }

    public string Decision { get; set; } = string.Empty;

    public bool CorrectnessChecked { get; set; }

    public bool ImageRightsChecked { get; set; }

    public bool PersonalDataChecked { get; set; }

    public bool DuplicateChecked { get; set; }

    public string Note { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
