// <copyright file="SolutionVerifierTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Ai;
using LearnPip.Api.Questions;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für den Lösungsvergleich.
/// </summary>
public sealed class SolutionVerifierTests
{
    /// <summary>
    /// Prüft unterstützte Dezimalrechnungen und die Kennzeichnung ungeprüfter Aufgaben.
    /// </summary>
    /// <param name="formula">Die zu prüfende Rechenaufgabe.</param>
    /// <param name="solution">Die angegebene Lösung.</param>
    /// <param name="reference">Die Referenzlösung, sofern vorhanden.</param>
    /// <param name="answer">Die Antwort, die der Hinweis nicht vorwegnehmen darf.</param>
    /// <param name="status">Der erwartete Status des Lösungsvergleichs.</param>
    [Theory]
    [InlineData("2 + 3 * 4 = ?", "14", "14", "14", "verified")]
    [InlineData("(2 + 3) * 4", "20", null, "20", "verified")]
    [InlineData("2 + 3 * 4", "20", null, "20", "conflict")]
    [InlineData("8 / 0", "0", null, "0", "unverified")]
    [InlineData("10 m / 2", "5", null, "5", "unverified")]
    [InlineData("2 + 2", "4", "5", "4", "conflict")]
    [InlineData("Biologie: Mitochondrien", "Zellatmung", null, "Zellatmung", "unverified")]
    public void ArithmeticIsCheckedButUnsupportedSubjectsAreNotClaimedVerified(
        string formula,
        string solution,
        string? reference,
        string answer,
        string status)
    {
        Assert.Equal(status, SolutionVerifier.Check(formula, solution, reference, answer).Status);
    }

    /// <summary>
    /// Prüft die Ablehnung widersprüchlicher Lösungsschritte und vorweggenommener Antworten.
    /// </summary>
    [Fact]
    public void ContradictoryStepsAndSpoilerHintsAreWithheld()
    {
        var actualResult1 = SolutionVerifier.Check(
                    "2+2",
                    "4",
                    null,
                    "4",
                    questionText: "2+3=?").Status;
        Assert.Equal(
                    "conflict",
                    actualResult1);
        var actualResult2 = SolutionVerifier.Check(
                    "2+2",
                    "4",
                    null,
                    "4",
                    ["2+2=5"]).Status;
        Assert.Equal(
                    "conflict",
                    actualResult2);
        Assert.False(SolutionVerifier.SafeHint("Das Ergebnis ist 4.", "4"));
        Assert.True(SolutionVerifier.SafeHint("Zähle die beiden Summanden.", "4"));
        var explanation = new[]
        {
            new ContentBlockOutput(
            "text",
            "[Hinweis] Denke an 4.\n[Nächster Schritt] " + "Zähle zuerst die Summanden.\nDie Lösung ist 4.",
            null,
            null),
        };
        var answers = new[]
        {
            new AnswerOutput(
            Guid.NewGuid(),
            true,
            [new ContentBlockOutput("text", "4", null, null)]),
        };
        var guidance = LearningSessionEndpoints.Guidance(explanation, answers);
        Assert.Null(guidance.Hint);
        Assert.Equal("Zähle zuerst die Summanden.", guidance.NextStep);
    }
}
