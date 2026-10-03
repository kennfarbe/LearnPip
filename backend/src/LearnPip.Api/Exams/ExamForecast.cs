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

/// <summary>
/// Konservative Prognose der Prüfungsbereitschaft mit Begründungen.
/// </summary>
/// <param name="Status">Der Status der Operation oder Prognose.</param>
/// <param name="EarliestReadyDate">Der früheste prognostizierte Termin der Prüfungsbereitschaft.</param>
/// <param name="LatestReadyDate">Der späteste prognostizierte Termin der Prüfungsbereitschaft.</param>
/// <param name="SuggestedExamDate">Der vorgeschlagene Prüfungstermin.</param>
/// <param name="Evidence">Die zugrunde liegenden Lern- und Simulationsdaten.</param>
/// <param name="Reasons">Die Begründungen und Einschränkungen der Prognose.</param>
/// <param name="Assumptions">Die für den Lösungsweg getroffenen Annahmen.</param>
/// <param name="FormalAdmissionStatus">Der bekannte Status der formalen Prüfungszulassung.</param>
/// <param name="RulesSourceUrl">Die Adresse der maßgeblichen Prüfungsregeln.</param>
/// <param name="RulesCheckedOn">Das Datum der letzten Regelprüfung.</param>
/// <param name="ProfileVersion">Die Nummer der Prüfungsprofilversion.</param>
/// <param name="Sessions">Die berücksichtigten Prüfungstermine.</param>
public sealed record ExamForecast(
        string Status,
        DateOnly? EarliestReadyDate,
        DateOnly? LatestReadyDate,
        DateOnly? SuggestedExamDate,
        ForecastEvidence Evidence,
        IReadOnlyList<string> Reasons,
        string Assumptions,
        string FormalAdmissionStatus,
        string? RulesSourceUrl,
        DateOnly? RulesCheckedOn,
        int ProfileVersion,
        IReadOnlyList<ForecastSession> Sessions);
