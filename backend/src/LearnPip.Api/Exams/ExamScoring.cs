// <copyright file="ExamScoring.cs" company="LearnPip contributors">
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
/// Bewertet die Antworten einer Prüfungssimulation anhand des Profil-Snapshots.
/// </summary>
public static class ExamScoring
{
    /// <summary>
    /// Bewertet die Antworten eines Prüfungsteils anhand seiner gespeicherten Regeln.
    /// </summary>
    /// <param name="part">Der zu bewertende Prüfungsteil.</param>
    /// <param name="answers">Die ausgewählten Antworten nach Fragenkennung.</param>
    /// <param name="timedOut">Gibt an, ob die Zeitgrenze überschritten wurde.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static PartResult Score(
        SnapshotPart part,
        IReadOnlyDictionary<string, int> answers,
        bool timedOut)
    {
        if (part.Credited)
        {
            return new PartResult(part.Rule.Code, 0, part.Rule.QuestionCount, true, true, false);
        }

        var correct = part.Questions.Count(question =>
            answers.TryGetValue(question.Code, out var index) && index == question.CorrectIndex);
        return new PartResult(
            part.Rule.Code,
            correct,
            part.Rule.QuestionCount,
            !timedOut && correct >= part.Rule.RequiredCorrect,
            false,
            timedOut);
    }

    /// <summary>
    /// Gibt an, ob die Bestehensgrenze erreicht wurde.
    /// </summary>
    /// <param name="parts">Die gespeicherten Prüfungsteile.</param>
    /// <param name="results">Die Bewertungen der einzelnen Prüfungsteile.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool Passed(IReadOnlyList<SnapshotPart> parts, IReadOnlyList<PartResult> results) =>
        parts.All(part => results.Any(result => result.Code == part.Rule.Code && result.Passed));
}
