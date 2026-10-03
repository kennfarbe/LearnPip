// <copyright file="PhotoCheckInput.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;

/// <summary>
/// Anfrage zur Prüfung eines aus einem Foto erkannten Lösungswegs.
/// </summary>
/// <param name="Formula">Die zu prüfende Formel, sofern vorhanden.</param>
/// <param name="ComputedSolution">Die berechnete Lösung, sofern vorhanden.</param>
/// <param name="ReferenceSolution">Die Referenzlösung, sofern vorhanden.</param>
/// <param name="ChosenAnswer">Die vom Benutzer bestätigte Antwort.</param>
/// <param name="Steps">Die erkannten oder vorgeschlagenen Lösungsschritte.</param>
/// <param name="QuestionText">Die erkannte Fragenformulierung.</param>
public sealed record PhotoCheckInput(
        string? Formula,
        string? ComputedSolution,
        string? ReferenceSolution,
        string? ChosenAnswer,
        IReadOnlyList<string>? Steps,
        string? QuestionText);
