// <copyright file="LearningContentView.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Lerninhalt mit persönlichem Wiederholungsstand.
/// </summary>
/// <param name="Id">Die eindeutige Kennung.</param>
/// <param name="Title">Der Titel.</param>
/// <param name="QuestionIds">Die Kennungen der ausgewählten Fragen.</param>
/// <param name="OftenForMe">Gibt an, ob der Inhalt häufig wiederholt wird.</param>
/// <param name="ConfidentStreak">Die Anzahl aufeinanderfolgender sicherer Antworten.</param>
/// <param name="DueAtUtc">Der nächste Fälligkeitszeitpunkt in UTC, sofern vorhanden.</param>
/// <param name="Mastered">Gibt an, ob der Inhalt sicher beherrscht wird.</param>
/// <param name="Answers">Die Anzahl oder Zuordnung der Antworten.</param>
/// <param name="Guesses">Die Anzahl geratener Antworten.</param>
/// <param name="ExplanationsViewed">Die Anzahl angesehener Erklärungen.</param>
public sealed record LearningContentView(
        Guid Id,
        string Title,
        IReadOnlyList<Guid> QuestionIds,
        bool OftenForMe,
        int ConfidentStreak,
        DateTimeOffset? DueAtUtc,
        bool Mastered,
        int Answers,
        int Guesses,
        int ExplanationsViewed);
