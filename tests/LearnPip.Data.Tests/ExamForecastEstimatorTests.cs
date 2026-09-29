using LearnPip.Api.Exams;

namespace LearnPip.Data.Tests;

public sealed class ExamForecastEstimatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 29);
    private static readonly ForecastEvidence Evidence = new(80, 100, 7, 10, 2, 2, 2);
    private static readonly SimulationSignal[] Passed =
    [
        new(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero), true),
        new(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero), true)
    ];

    [Fact]
    public void Readiness_requires_coverage_spaced_repeats_and_two_recent_passes()
    {
        var estimate = ExamForecastEstimator.Estimate(Evidence, Passed, [], 1, null, null, Today);
        Assert.Equal("window", estimate.Status);
        Assert.Equal(Today.AddDays(7), estimate.EarliestReadyDate);
        Assert.Null(estimate.SuggestedExamDate);
        Assert.Equal("insufficient", ExamForecastEstimator.Estimate(Evidence with
        {
            AnsweredCatalogQuestions = 79
        }, Passed, [], 1, null, null, Today).Status);
        Assert.Equal("insufficient", ExamForecastEstimator.Estimate(Evidence with
        {
            SpacedMasteredContents = 6
        }, Passed, [], 1, null, null, Today).Status);
        Assert.Equal("insufficient", ExamForecastEstimator.Estimate(Evidence,
            [Passed[0], Passed[1] with { Passed = false }], [], 1, null, null, Today).Status);
        Assert.Equal("insufficient", ExamForecastEstimator.Estimate(Evidence,
            [Passed[0] with { CompletedAtUtc = Passed[0].CompletedAtUtc.AddDays(-31) },
                Passed[1]], [], 1, null, null, Today).Status);
    }

    [Fact]
    public void Only_recent_sourced_dates_with_open_known_deadline_are_suggested()
    {
        var sessions = new[]
        {
            new ExamSession(Today.AddDays(8), "Berlin", Today.AddDays(2),
                "https://example.org/schedule", Today.AddDays(-31)),
            new ExamSession(Today.AddDays(10), "Bonn", null,
                "https://example.org/schedule", Today),
            new ExamSession(Today.AddDays(12), "Köln", Today.AddDays(4),
                "https://example.org/schedule", Today)
        };
        var result = ExamForecastEstimator.Estimate(Evidence, Passed, sessions, 3,
            "https://example.org/rules", Today, Today);
        Assert.Equal(Today.AddDays(12), result.SuggestedExamDate);
        Assert.Equal("unknown", result.Sessions[1].RegistrationStatus);
        Assert.Contains("Nicht geprüft", result.FormalAdmissionStatus);
        Assert.Null(ExamForecastEstimator.Estimate(Evidence, Passed, sessions, 3,
            "https://example.org/rules", Today.AddDays(-31), Today).SuggestedExamDate);
    }
}
