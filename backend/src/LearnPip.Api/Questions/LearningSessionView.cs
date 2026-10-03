// <copyright file="LearningSessionView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;
/// <summary>
/// Fortschritt und Fragen einer Lernsitzung.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Total">Die Gesamtanzahl der Einträge oder Fragen.</param>
/// <param name="Answered">Die Anzahl beantworteter Fragen.</param>
/// <param name="Skipped">Die Anzahl übersprungener Fragen.</param>
/// <param name="Completed">Gibt an, ob die Sitzung abgeschlossen ist.</param>
/// <param name="Current">Die aktuelle Lernfrage, sofern vorhanden.</param>
public sealed record LearningSessionView(
        Guid Id,
        int Total,
        int Answered,
        int Skipped,
        bool Completed,
        LearningQuestion? Current);
