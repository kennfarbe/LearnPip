// <copyright file="ReviewOverview.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Übersicht des persönlichen Wiederholungsstands.
/// </summary>
/// <param name="TotalContents">Die Anzahl der berücksichtigten Lerninhalte.</param>
/// <param name="MasteredContents">Die Anzahl sicher beherrschter Inhalte.</param>
/// <param name="OftenForMeCount">Die Anzahl häufig wiederholter Inhalte.</param>
/// <param name="Contents">Die Lerninhalte mit Wiederholungsstand.</param>
public sealed record ReviewOverview(
        int TotalContents,
        int MasteredContents,
        int OftenForMeCount,
        IReadOnlyList<LearningContentView> Contents);
