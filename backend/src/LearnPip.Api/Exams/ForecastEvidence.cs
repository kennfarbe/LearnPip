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

/// <summary>
/// Lern- und Simulationsdaten als Grundlage der Prüfungsprognose.
/// </summary>
/// <param name="AnsweredCatalogQuestions">Die Anzahl bereits beantworteter Katalogfragen.</param>
/// <param name="CatalogQuestions">Die Gesamtanzahl der Katalogfragen.</param>
/// <param name="SpacedMasteredContents">Die Anzahl durch zeitlich getrennte Antworten beherrschter Inhalte.</param>
/// <param name="OwnContents">Die Anzahl selbst erstellter Inhalte.</param>
/// <param name="CompletedSimulations">Die Anzahl abgeschlossener Prüfungssimulationen.</param>
/// <param name="RecentSimulations">Die Anzahl aktueller Prüfungssimulationen.</param>
/// <param name="RecentPassedSimulations">Die Anzahl zuletzt bestandener Prüfungssimulationen.</param>
public sealed record ForecastEvidence(
        int AnsweredCatalogQuestions,
        int CatalogQuestions,
        int SpacedMasteredContents,
        int OwnContents,
        int CompletedSimulations,
        int RecentSimulations,
        int RecentPassedSimulations);
