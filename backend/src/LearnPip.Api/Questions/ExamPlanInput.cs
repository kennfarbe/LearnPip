// <copyright file="ExamPlanInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

public sealed record ExamPlanInput(
    int? ScopeContents, int? DailyMinutes, int? TargetPercent,
    DateOnly? ExamDate, int? HorizonDays, int DailyLimitMinutes,
    IReadOnlyList<int>? SchoolDays, IReadOnlyList<DateOnly>? BreakDays,
    IReadOnlyList<Guid>? ContentIds);
