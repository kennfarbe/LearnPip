// <copyright file="PhotoRecognition.cs" company="LearnPip contributors">
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
/// Aus einem Foto erkannte Frage mit Lösungsweg und Unsicherheiten.
/// </summary>
/// <param name="DetectedText">Der auf dem Foto erkannte Text.</param>
/// <param name="QuestionText">Die erkannte Fragenformulierung.</param>
/// <param name="Subject">Das Fach oder Themengebiet.</param>
/// <param name="Topic">Das Thema innerhalb des Fachs.</param>
/// <param name="Formula">Die zu prüfende Formel, sofern vorhanden.</param>
/// <param name="DrawingDescription">Die Beschreibung einer erkannten Zeichnung.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
/// <param name="SuggestedCorrectIndex">Der vorgeschlagene Index der richtigen Antwort.</param>
/// <param name="ComputedSolution">Die berechnete Lösung, sofern vorhanden.</param>
/// <param name="Steps">Die erkannten oder vorgeschlagenen Lösungsschritte.</param>
/// <param name="ReferenceSolution">Die Referenzlösung, sofern vorhanden.</param>
/// <param name="Uncertainties">Die Unsicherheiten der Bilderkennung.</param>
/// <param name="Hint">Der Lernhinweis, sofern vorhanden.</param>
/// <param name="NextStep">Der vorgeschlagene nächste Lernschritt, sofern vorhanden.</param>
public sealed record PhotoRecognition(
        string DetectedText,
        string QuestionText,
        string Subject,
        string Topic,
        string? Formula,
        string? DrawingDescription,
        IReadOnlyList<string> Answers,
        int? SuggestedCorrectIndex,
        string ComputedSolution,
        IReadOnlyList<string> Steps,
        string? ReferenceSolution,
        IReadOnlyList<string> Uncertainties,
        string? Hint = null,
        string? NextStep = null);
