// <copyright file="ProgressAttempt.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Antwortversuch zur Berechnung von Teilnahme und Lernfortschritt.
/// </summary>
/// <param name="ContentId">Die Kennung des Lerninhalts.</param>
/// <param name="SessionId">Die Kennung der Lernsitzung.</param>
/// <param name="AnsweredAtUtc">Der Zeitpunkt der Antwort in UTC.</param>
/// <param name="IsCorrect">Gibt an, ob die Antwort richtig ist.</param>
/// <param name="WasGuessed">Gibt an, ob die Antwort geraten wurde.</param>
/// <param name="ExplanationViewedAtUtc">Der Zeitpunkt der angesehenen Erklärung in UTC, sofern vorhanden.</param>
public sealed record ProgressAttempt(
        Guid ContentId,
        Guid SessionId,
        DateTimeOffset AnsweredAtUtc,
        bool IsCorrect,
        bool WasGuessed,
        DateTimeOffset? ExplanationViewedAtUtc);
