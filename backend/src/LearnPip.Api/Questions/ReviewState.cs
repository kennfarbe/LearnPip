// <copyright file="ReviewState.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;
/// <summary>
/// Aktueller Wiederholungsstand einschließlich nächster Fälligkeit.
/// </summary>
/// <param name="ConfidentStreak">Die Anzahl aufeinanderfolgender sicherer Antworten.</param>
/// <param name="DueAtUtc">Der nächste Fälligkeitszeitpunkt in UTC, sofern vorhanden.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
/// <param name="Guesses">Die Anzahl geratener Antworten.</param>
/// <param name="ExplanationsViewed">Die Anzahl angesehener Erklärungen.</param>
/// <param name="Mastered">Gibt an, ob der Inhalt sicher beherrscht wird.</param>
public sealed record ReviewState(
        int ConfidentStreak,
        DateTimeOffset? DueAtUtc,
        int Answers,
        int Guesses,
        int ExplanationsViewed,
        bool Mastered);
