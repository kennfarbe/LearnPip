// <copyright file="ExamQuestionSelection.cs" company="LearnPip contributors">
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
/// Wählt Prüfungsfragen nach Profil und Fragenmodus aus.
/// </summary>
public static class ExamQuestionSelection
{
    /// <summary>
    /// Wählt die für einen Prüfungstest benötigten Katalogfragen aus.
    /// </summary>
    /// <param name="catalog">Die verfügbaren Fragen des Prüfungskatalogs.</param>
    /// <param name="rule">Die Bewertungs- und Auswahlregeln des Prüfungsteils.</param>
    /// <param name="mode">Der ausgewählte KI-Betriebsmodus.</param>
    /// <param name="count">Die gewünschte Anzahl von Fragen.</param>
    /// <param name="random">Die Zufallsquelle für die Fragenauswahl.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IReadOnlyList<CatalogQuestion> Select(
        IReadOnlyList<CatalogQuestion> catalog,
        ProfilePart rule,
        string mode,
        int? count,
        Random random)
    {
        if (mode is not ("original" or "variant" or "mixed") ||
            (mode != "original" && !rule.AllowVariants))
        {
            throw new ArgumentException("Question mode unavailable.");
        }

        var groups = catalog.Where(question => question.PartCode == rule.CatalogPartCode)
            .GroupBy(question => question.BaseCode ?? question.Code).ToArray();
        var candidates = groups.Select(group =>
        {
            var originals = group.Where(question => question.Kind == "original").ToArray();
            var variants = group.Where(question => question.Kind == "variant").ToArray();
            var pool = mode switch
            {
                "original" => originals,
                "variant" => variants,
                _ => variants.Length > 0 && random.Next(2) == 0 ? variants : originals,
            };
            return pool.Length == 0 ? null : pool[random.Next(pool.Length)];
        }).Where(question => question != null).Cast<CatalogQuestion>()
            .OrderBy(_ => random.Next()).ToArray();
        if (count is > 0 && candidates.Length < count)
        {
            throw new ArgumentException("Too few questions for this mode.");
        }

        return candidates.Take(count ?? candidates.Length).Select(question =>
        {
            if (!rule.ShuffleAnswers)
            {
                return question;
            }

            var order = Enumerable.Range(0, question.Answers.Count).OrderBy(_ => random.Next())
                .ToArray();
            return question with
            {
                Answers = order.Select(index => question.Answers[index]).ToArray(),
                CorrectIndex = Array.IndexOf(order, question.CorrectIndex),
            };
        }).ToArray();
    }
}
