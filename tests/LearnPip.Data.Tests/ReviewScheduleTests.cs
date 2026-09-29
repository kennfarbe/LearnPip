using LearnPip.Api.Questions;

namespace LearnPip.Data.Tests;

public sealed class ReviewScheduleTests
{
    [Fact]
    public void Replay_is_reproducible_and_uncertainty_reduces_interval()
    {
        var start = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var events = Enumerable.Range(0, 3).Select(index => new ReviewEvent(Guid.Parse(
            $"00000000-0000-0000-0000-{index + 1:000000000000}"), start.AddDays(index * 4),
            "answer", true)).ToArray();
        var mastered = ReviewSchedule.Replay(events);
        Assert.True(mastered.Mastered);
        Assert.Equal(3, mastered.ConfidentStreak);
        Assert.Equal(start.AddDays(15), mastered.DueAtUtc);
        Assert.Equal(mastered, ReviewSchedule.Replay(events.Reverse()));

        var guess = new ReviewEvent(events[2].AttemptId, events[2].AtUtc, "guess", false);
        var unsure = ReviewSchedule.Replay([.. events, guess]);
        Assert.False(unsure.Mastered);
        Assert.Equal(start.AddDays(9), unsure.DueAtUtc);
        Assert.Equal(1, unsure.Guesses);
        var explained = ReviewSchedule.Replay([.. events, new ReviewEvent(events[2].AttemptId,
            start.AddDays(9), "explanation", false)]);
        Assert.Equal(start.AddDays(10), explained.DueAtUtc);
        Assert.Equal(1, explained.ExplanationsViewed);
    }
}
