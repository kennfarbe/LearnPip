// <copyright file="ReviewState.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;
public sealed record ReviewState(int ConfidentStreak, DateTimeOffset? DueAtUtc, int Answers,
    int Guesses, int ExplanationsViewed, bool Mastered);
