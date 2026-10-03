// <copyright file="LearningProgress.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Zusammengefasster persönlicher Lernfortschritt mit Themen und Wochenübersicht.
/// </summary>
/// <param name="TotalContents">Die Anzahl der berücksichtigten Lerninhalte.</param>
/// <param name="MasteredContents">Die Anzahl sicher beherrschter Inhalte.</param>
/// <param name="ImprovedContents">Die Anzahl verbesserter Lerninhalte.</param>
/// <param name="ParticipationPoints">Die erreichten Teilnahmepunkte.</param>
/// <param name="LearningDays">Die Anzahl der berücksichtigten Lerntage.</param>
/// <param name="Topics">Die Lernstände nach Fach und Thema.</param>
/// <param name="RecentWeeks">Die Teilnahmeübersicht der letzten Wochen.</param>
public sealed record LearningProgress(
        int TotalContents,
        int MasteredContents,
        int ImprovedContents,
        int ParticipationPoints,
        int LearningDays,
        IReadOnlyList<TopicProgress> Topics,
        IReadOnlyList<ProgressWeek> RecentWeeks);
