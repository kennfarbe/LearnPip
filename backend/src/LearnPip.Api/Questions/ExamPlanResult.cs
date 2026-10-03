// <copyright file="ExamPlanResult.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

public sealed record ExamPlanResult(
    string EstimatedDimension, int ScopeContents, int DailyMinutes, int TargetPercent,
    int AvailableContents, int MasteredContents, int PlanningDays, int StudyDays,
    int ExpectedNewlyMastered, int ConservativeNewlyMastered,
    int ExpectedQuotePercent, int ConservativeQuotePercent, bool Feasible,
    int? SuggestedDailyMinutes, int? SuggestedScopeContents, DateOnly? SuggestedExamDate,
    IReadOnlyList<string> Options, string Assumptions);
