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

/// <summary>
/// Fortschritt und Ergebnis eines intensiven Prüfungstests.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="ProfileVersionId">Die Kennung der Prüfungsprofilversion.</param>
/// <param name="ProfileCode">Der Bezeichner des Prüfungsprofils.</param>
/// <param name="CatalogRevision">Die Revision des Prüfungskatalogs.</param>
/// <param name="QuestionMode">Der Modus für Originalfragen oder Varianten.</param>
/// <param name="Stage">Die aktuelle Teststufe.</param>
/// <param name="Stages">Die Stufen des intensiven Tests.</param>
/// <param name="CompletedAtUtc">Der Abschlusszeitpunkt in UTC, sofern vorhanden.</param>
/// <param name="Questions">Die ausgewählten Prüfungsfragen.</param>
/// <param name="SelectedAnswers">Die ausgewählten Antworten.</param>
/// <param name="Parts">Die Prüfungsteile.</param>
/// <param name="Mistakes">Die erfassten Fehler des Tests.</param>
public sealed record PowerView(
        Guid Id,
        Guid ProfileVersionId,
        string ProfileCode,
        string CatalogRevision,
        string QuestionMode,
        int Stage,
        int Stages,
        DateTimeOffset? CompletedAtUtc,
        IReadOnlyList<object> Questions,
        IReadOnlyDictionary<string, int> SelectedAnswers,
        IReadOnlyList<PowerPartResult> Parts,
        IReadOnlyList<PowerMistake> Mistakes);
