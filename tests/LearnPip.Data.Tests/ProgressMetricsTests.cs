using LearnPip.Api.Questions;

namespace LearnPip.Data.Tests;

public sealed class ProgressMetricsTests
{
    [Fact]
    public void Points_are_capped_per_content_and_day_and_pauses_do_not_remove_them()
    {
        var content = Guid.NewGuid();
        var day = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        ProgressAttempt[] attempts =
        [
            new(content, Guid.NewGuid(), day, false, true, null),
            new(content, Guid.NewGuid(), day.AddHours(1), true, false, day.AddHours(2)),
            new(content, Guid.NewGuid(), day.AddHours(3), true, false, null)
        ];
        Assert.Equal(4, ProgressMetrics.ParticipationPoints(attempts));
        Assert.Equal(4, ProgressMetrics.ParticipationPoints(attempts.Reverse()));
        Assert.Equal(7, ProgressMetrics.ParticipationPoints([.. attempts,
            new ProgressAttempt(content, Guid.NewGuid(), day.AddDays(30), true, false, null)]));
        Assert.True(ProgressMetrics.HasImproved(attempts));
    }

    [Fact]
    public void Explanation_after_a_safe_answer_is_not_a_past_difficulty()
    {
        var content = Guid.NewGuid();
        var day = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var first = new ProgressAttempt(content, Guid.NewGuid(), day, true, false, day.AddDays(2));
        var second = new ProgressAttempt(content, Guid.NewGuid(), day.AddDays(1), true, false, null);
        Assert.False(ProgressMetrics.HasImproved([first, second]));
        var third = new ProgressAttempt(content, Guid.NewGuid(), day.AddDays(3), true, false, null);
        Assert.True(ProgressMetrics.HasImproved([first, second, third]));
    }
}
