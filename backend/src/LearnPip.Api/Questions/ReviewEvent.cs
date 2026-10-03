// <copyright file="ReviewEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

public sealed record ReviewEvent(Guid AttemptId, DateTimeOffset AtUtc, string Kind, bool IsCorrect);
