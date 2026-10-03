// <copyright file="ReviewEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Antwortereignis zur Rekonstruktion eines Wiederholungsplans.
/// </summary>
/// <param name="AttemptId">Die Kennung des Antwortversuchs.</param>
/// <param name="AtUtc">Der Zeitpunkt des Ereignisses in UTC.</param>
/// <param name="Kind">Die Art des Inhaltsblocks oder Ereignisses.</param>
/// <param name="IsCorrect">Gibt an, ob die Antwort richtig ist.</param>
public sealed record ReviewEvent(Guid AttemptId, DateTimeOffset AtUtc, string Kind, bool IsCorrect);
