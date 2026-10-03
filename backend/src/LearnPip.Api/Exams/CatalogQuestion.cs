// <copyright file="CatalogQuestion.cs" company="LearnPip contributors">
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
/// Frage eines Prüfungskatalogs mit Antwortoptionen und Quellenangabe.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="PartCode">Der Bezeichner des Prüfungsteils.</param>
/// <param name="Prompt">Die Text- oder Inhaltsblöcke der Frage.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
/// <param name="CorrectIndex">Der Index der richtigen Antwort.</param>
/// <param name="Kind">Die Art des Inhaltsblocks oder Ereignisses.</param>
/// <param name="BaseCode">Der Bezeichner der ursprünglichen Katalogfrage.</param>
public sealed record CatalogQuestion(
        string Code,
        string PartCode,
        string Prompt,
        IReadOnlyList<string> Answers,
        int CorrectIndex,
        string Kind = "original",
        string? BaseCode = null);
