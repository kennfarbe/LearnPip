// <copyright file="ProgressAttempt.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

public sealed record ProgressAttempt(Guid ContentId, Guid SessionId, DateTimeOffset AnsweredAtUtc,
    bool IsCorrect, bool WasGuessed, DateTimeOffset? ExplanationViewedAtUtc);
