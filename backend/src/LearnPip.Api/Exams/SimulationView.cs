// <copyright file="SimulationView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record SimulationView(Guid Id, Guid ProfileVersionId, int ProfileVersion,
    string ProfileCode, string CatalogRevision, DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? CurrentPartCode, DateTimeOffset? DeadlineAtUtc,
    IReadOnlyList<object> Questions, IReadOnlyList<PartResult> Parts, bool? Passed,
    IReadOnlyDictionary<string, int> SelectedAnswers);
