// <copyright file="PartResult.cs" company="LearnPip contributors">
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
/// Bewertung eines Prüfungsteils einschließlich Bestehensgrenze.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="Correct">Die Anzahl richtiger Antworten.</param>
/// <param name="Total">Die Gesamtanzahl der Einträge oder Fragen.</param>
/// <param name="Passed">Gibt an, ob die Bestehensgrenze erreicht wurde.</param>
/// <param name="Credited">Gibt an, ob der Prüfungsteil angerechnet wurde.</param>
/// <param name="TimedOut">Gibt an, ob die Zeitgrenze überschritten wurde.</param>
public sealed record PartResult(
        string Code,
        int Correct,
        int Total,
        bool Passed,
        bool Credited,
        bool TimedOut);
