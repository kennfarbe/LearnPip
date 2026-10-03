// <copyright file="LearningFeedback.cs" company="LearnPip contributors">
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
/// Rückmeldung zu einer Lernantwort mit Erklärung und nächstem Lernschritt.
/// </summary>
/// <param name="AttemptId">Die Kennung des Antwortversuchs.</param>
/// <param name="ContentId">Die Kennung des Lerninhalts.</param>
/// <param name="IsCorrect">Gibt an, ob die Antwort richtig ist.</param>
/// <param name="CorrectOptionIds">Die Kennungen der richtigen Antwortoptionen.</param>
/// <param name="Explanation">Die Erklärung zur Lösung.</param>
/// <param name="ShortExplanation">Die kurze Erklärung zur Lösung.</param>
public sealed record LearningFeedback(
        Guid AttemptId,
        Guid ContentId,
        bool IsCorrect,
        IReadOnlyList<Guid> CorrectOptionIds,
        IReadOnlyList<ContentBlockOutput> Explanation,
        string? ShortExplanation);
