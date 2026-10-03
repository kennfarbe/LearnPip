// <copyright file="ExamForecast.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record ExamForecast(string Status, DateOnly? EarliestReadyDate,
    DateOnly? LatestReadyDate, DateOnly? SuggestedExamDate, ForecastEvidence Evidence,
    IReadOnlyList<string> Reasons, string Assumptions, string FormalAdmissionStatus,
    string? RulesSourceUrl, DateOnly? RulesCheckedOn, int ProfileVersion,
    IReadOnlyList<ForecastSession> Sessions);
