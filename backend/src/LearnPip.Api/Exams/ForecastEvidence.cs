// <copyright file="ForecastEvidence.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public sealed record ForecastEvidence(int AnsweredCatalogQuestions, int CatalogQuestions,
    int SpacedMasteredContents, int OwnContents, int CompletedSimulations,
    int RecentSimulations, int RecentPassedSimulations);
