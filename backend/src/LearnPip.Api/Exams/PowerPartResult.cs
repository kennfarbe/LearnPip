// <copyright file="PowerPartResult.cs" company="LearnPip contributors">
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
/// Richtige und gesamte Antworten eines Prüfungsteils im intensiven Test.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="Correct">Die Anzahl richtiger Antworten.</param>
/// <param name="Total">Die Gesamtanzahl der Einträge oder Fragen.</param>
public sealed record PowerPartResult(string Code, int Correct, int Total);
