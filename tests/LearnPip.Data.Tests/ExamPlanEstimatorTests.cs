// <copyright file="ExamPlanEstimatorTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Questions;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für das Prüfungsdreieck.
/// </summary>
public sealed class ExamPlanEstimatorTests
{
    private static readonly DateOnly Start = new(2026, 9, 29);

    /// <summary>
    /// Prüft unterschiedliche Lerntage und Lerninhalte für sichere Wiederholungen.
    /// </summary>
    [Fact]
    public void Repetition_needs_separate_days_and_a_distinct_content()
    {
        var shortPlan = new ExamPlanInput(1, 120, null, null, 4, 120, [], [], null);
        var longPlan = shortPlan with { HorizonDays = 5 };
        Assert.Equal(0, ExamPlanEstimator.Estimate(shortPlan, 1, 0, Start).ExpectedNewlyMastered);
        Assert.Equal(1, ExamPlanEstimator.Estimate(longPlan, 1, 0, Start).ExpectedNewlyMastered);
        var id = Guid.NewGuid();
        Assert.False(ExamPlanEstimator.HasSpacedMastery(Enumerable.Range(
                    0,
                    3)
            .Select(index => new ReviewEvent(
                        Guid.NewGuid(),
                        new DateTimeOffset(2026, 9, 29, 12, index, 0, TimeSpan.Zero),
                        "answer",
                        true))));
        Assert.True(ExamPlanEstimator.HasSpacedMastery(new[]
        {
            new ReviewEvent(id, new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero), "answer", true),
            new ReviewEvent(Guid.NewGuid(), new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero), "answer", true),
            new ReviewEvent(
                    Guid.NewGuid(),
                    new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero),
                    "answer",
                    true),
        }));
    }

    /// <summary>
    /// Prüft die Auswirkungen von Pausen, Schultagen und Tageslimits auf die Machbarkeit des Prüfungstermins.
    /// </summary>
    [Fact]
    public void Breaks_school_days_and_daily_limit_make_an_optimistic_date_infeasible()
    {
        var breaks = Enumerable.Range(0, 4).Select(Start.AddDays).ToArray();
        var plan = new ExamPlanInput(2, 30, 100, null, 5, 30, [1, 2, 3, 4, 5], breaks, null);
        var result = ExamPlanEstimator.Estimate(plan with { TargetPercent = null }, 2, 0, Start);
        Assert.Equal(0, result.ExpectedQuotePercent);
        var target = ExamPlanEstimator.Estimate(plan with { DailyMinutes = null }, 2, 0, Start);
        Assert.False(target.Feasible);
        Assert.True(target.SuggestedExamDate > Start);
        Assert.Contains("Zielquote anpassen", target.Options);
    }

    /// <summary>
    /// Prüft die einmalige Anrechnung beherrschter Inhalte und die Begrenzung der Lernzeit.
    /// </summary>
    [Fact]
    public void Already_mastered_content_counts_once_and_time_is_bounded()
    {
        var plan = new ExamPlanInput(2, null, 50, null, 7, 60, [], [], null);
        var result = ExamPlanEstimator.Estimate(plan, 2, 1, Start);
        Assert.Equal(1, result.MasteredContents);
        Assert.InRange(result.DailyMinutes, 0, 60);
        Assert.True(result.Feasible);
    }
}
