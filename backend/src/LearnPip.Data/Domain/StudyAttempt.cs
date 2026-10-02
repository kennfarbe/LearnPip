// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class StudyAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudySessionId { get; set; }

    public Guid QuestionVersionId { get; set; }

    public bool IsCorrect { get; set; }

    public bool WasGuessed { get; set; }

    public DateTimeOffset? ExplanationViewedAtUtc { get; set; }

    public DateTimeOffset AnsweredAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public StudySession StudySession { get; set; } = null!;

    public QuestionVersion QuestionVersion { get; set; } = null!;

    public ICollection<StudyAttemptSelection> Selections { get; set; } = [];
}
