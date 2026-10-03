// <copyright file="LearningContentView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed record LearningContentView(Guid Id, string Title, IReadOnlyList<Guid> QuestionIds,
    bool OftenForMe, int ConfidentStreak, DateTimeOffset? DueAtUtc,
    bool Mastered, int Answers, int Guesses, int ExplanationsViewed);
