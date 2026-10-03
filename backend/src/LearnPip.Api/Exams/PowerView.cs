// <copyright file="PowerView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;
public sealed record PowerView(Guid Id, Guid ProfileVersionId, string ProfileCode,
    string CatalogRevision, string QuestionMode, int Stage, int Stages,
    DateTimeOffset? CompletedAtUtc, IReadOnlyList<object> Questions,
    IReadOnlyDictionary<string, int> SelectedAnswers,
    IReadOnlyList<PowerPartResult> Parts, IReadOnlyList<PowerMistake> Mistakes);
