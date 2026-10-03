// <copyright file="ExamForecastEstimatorTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Exams;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für die Prüfungsprognose.
/// </summary>
public sealed class ExamForecastEstimatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 29);
    private static readonly ForecastEvidence Evidence = new(80, 100, 7, 10, 2, 2, 2);
    private static readonly SimulationSignal[] Passed =
    [
        new(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero), true),
        new(
        new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero),
        true)
    ];

    /// <summary>
    /// Prüft Fragenabdeckung, zeitversetzte Wiederholungen und zwei aktuelle bestandene Simulationen als Prognosegrundlage.
    /// </summary>
    [Fact]
    public void ReadinessRequiresCoverageSpacedRepeatsAndTwoRecentPasses()
    {
        var estimate = ExamForecastEstimator.Estimate(Evidence, Passed, [], 1, null, null, Today);
        Assert.Equal("window", estimate.Status);
        Assert.Equal(Today.AddDays(7), estimate.EarliestReadyDate);
        Assert.Null(estimate.SuggestedExamDate);
        var actualResult1 = ExamForecastEstimator.Estimate(
            Evidence with
            {
                AnsweredCatalogQuestions = 79,
            },
            Passed,
            [],
            1,
            null,
            null,
            Today).Status;
        Assert.Equal(
            "insufficient",
            actualResult1);
        var actualResult2 = ExamForecastEstimator.Estimate(
            Evidence with
            {
                SpacedMasteredContents = 6,
            },
            Passed,
            [],
            1,
            null,
            null,
            Today).Status;
        Assert.Equal(
            "insufficient",
            actualResult2);
        var actualResult3 = ExamForecastEstimator.Estimate(
            Evidence,
            [Passed[0], Passed[1] with { Passed = false }],
            [],
            1,
            null,
            null,
            Today).Status;
        Assert.Equal(
            "insufficient",
            actualResult3);
        var actualResult4 = ExamForecastEstimator.Estimate(
            Evidence,
            [Passed[0] with { CompletedAtUtc = Passed[0].CompletedAtUtc.AddDays(-31) },
                Passed[1]],
            [],
            1,
            null,
            null,
            Today).Status;
        Assert.Equal(
            "insufficient",
            actualResult4);
    }

    /// <summary>
    /// Prüft die Empfehlung aktueller Prüfungstermine mit Quellenangabe und bekannter offener Anmeldefrist.
    /// </summary>
    [Fact]
    public void OnlyRecentSourcedDatesWithOpenKnownDeadlineAreSuggested()
    {
        var sessions = new[]
        {
            new ExamSession(
            Today.AddDays(8),
            "Berlin",
            Today.AddDays(2),
            "https://example.org/schedule",
            Today.AddDays(-31)),
            new ExamSession(
            Today.AddDays(10),
            "Bonn",
            null,
            "https://example.org/schedule",
            Today),
            new ExamSession(
            Today.AddDays(12),
            "Köln",
            Today.AddDays(4),
            "https://example.org/schedule",
            Today),
        };
        var result = ExamForecastEstimator.Estimate(
            Evidence,
            Passed,
            sessions,
            3,
            "https://example.org/rules",
            Today,
            Today);
        Assert.Equal(Today.AddDays(12), result.SuggestedExamDate);
        Assert.Equal("unknown", result.Sessions[1].RegistrationStatus);
        Assert.Contains("Nicht geprüft", result.FormalAdmissionStatus);
        Assert.Null(ExamForecastEstimator.Estimate(
                Evidence,
                Passed,
                sessions,
                3,
                "https://example.org/rules",
                Today.AddDays(-31),
                Today).SuggestedExamDate);
    }
}
