// <copyright file="SimulationSignal.cs" company="LearnPip contributors">
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
/// Bestehenssignal einer abgeschlossenen Prüfungssimulation.
/// </summary>
/// <param name="CompletedAtUtc">Der Abschlusszeitpunkt in UTC, sofern vorhanden.</param>
/// <param name="Passed">Gibt an, ob die Bestehensgrenze erreicht wurde.</param>
public sealed record SimulationSignal(DateTimeOffset CompletedAtUtc, bool Passed);
