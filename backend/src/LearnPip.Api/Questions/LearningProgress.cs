// <copyright file="LearningProgress.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
public sealed record LearningProgress(int TotalContents, int MasteredContents,
    int ImprovedContents, int ParticipationPoints, int LearningDays,
    IReadOnlyList<TopicProgress> Topics, IReadOnlyList<ProgressWeek> RecentWeeks);
