// <copyright file="ProfilePart.cs" company="LearnPip contributors">
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
/// Bewertungs- und Zeitregeln eines Prüfungsteils.
/// </summary>
/// <param name="Code">Der fachliche Bezeichner oder Bestätigungscode.</param>
/// <param name="Title">Der Titel.</param>
/// <param name="CatalogPartCode">Der Bezeichner des zugehörigen Katalogteils.</param>
/// <param name="QuestionCount">Die Anzahl der Fragen.</param>
/// <param name="TimeLimitMinutes">Die Zeitgrenze in Minuten.</param>
/// <param name="RequiredCorrect">Die zum Bestehen erforderliche Anzahl richtiger Antworten.</param>
/// <param name="CreditCode">Der Bezeichner für die Anrechnung eines Prüfungsteils.</param>
/// <param name="AllowVariants">Gibt an, ob Fragenvarianten zulässig sind.</param>
/// <param name="ShuffleAnswers">Gibt an, ob Antwortoptionen zufällig angeordnet werden.</param>
public sealed record ProfilePart(
        string Code,
        string Title,
        string CatalogPartCode,
        int QuestionCount,
        int TimeLimitMinutes,
        int RequiredCorrect,
        string? CreditCode,
        bool AllowVariants = false,
        bool ShuffleAnswers = true);
