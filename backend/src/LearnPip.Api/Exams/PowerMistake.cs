// <copyright file="PowerMistake.cs" company="LearnPip contributors">
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
/// Fehlerhafte Antwort während eines intensiven Prüfungstests.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="PartCode">Der Bezeichner des Prüfungsteils.</param>
/// <param name="Prompt">Die Text- oder Inhaltsblöcke der Frage.</param>
/// <param name="SelectedAnswer">Die ausgewählte Antwort.</param>
/// <param name="CorrectAnswer">Die richtige Antwort.</param>
public sealed record PowerMistake(
        string Code,
        string PartCode,
        string Prompt,
        string? SelectedAnswer,
        string CorrectAnswer);
