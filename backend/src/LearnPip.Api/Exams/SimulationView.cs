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

/// <summary>
/// Fortschritt und Ergebnis einer Prüfungssimulation.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="ProfileVersionId">Die Kennung der Prüfungsprofilversion.</param>
/// <param name="ProfileVersion">Die Nummer der Prüfungsprofilversion.</param>
/// <param name="ProfileCode">Der Bezeichner des Prüfungsprofils.</param>
/// <param name="CatalogRevision">Die Revision des Prüfungskatalogs.</param>
/// <param name="StartedAtUtc">Der Startzeitpunkt in UTC.</param>
/// <param name="CompletedAtUtc">Der Abschlusszeitpunkt in UTC, sofern vorhanden.</param>
/// <param name="CurrentPartCode">Der Bezeichner des aktuellen Prüfungsteils.</param>
/// <param name="DeadlineAtUtc">Die Zeitgrenze des Prüfungsteils in UTC.</param>
/// <param name="Questions">Die ausgewählten Prüfungsfragen.</param>
/// <param name="Parts">Die Prüfungsteile.</param>
/// <param name="Passed">Gibt an, ob die Bestehensgrenze erreicht wurde.</param>
/// <param name="SelectedAnswers">Die ausgewählten Antworten.</param>
public sealed record SimulationView(
        Guid Id,
        Guid ProfileVersionId,
        int ProfileVersion,
        string ProfileCode,
        string CatalogRevision,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset? CompletedAtUtc,
        string? CurrentPartCode,
        DateTimeOffset? DeadlineAtUtc,
        IReadOnlyList<object> Questions,
        IReadOnlyList<PartResult> Parts,
        bool? Passed,
        IReadOnlyDictionary<string, int> SelectedAnswers);
