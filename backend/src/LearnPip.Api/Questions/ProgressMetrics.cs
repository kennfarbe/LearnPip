namespace LearnPip.Api.Questions;

public sealed record ProgressAttempt(Guid ContentId, Guid SessionId, DateTimeOffset AnsweredAtUtc,
    bool IsCorrect, bool WasGuessed, DateTimeOffset? ExplanationViewedAtUtc);

public static class ProgressMetrics
{
    // A day/content pair earns at most four points. Repeating clicks on the same
    // question or variant does not accumulate points; time spent is irrelevant.
    public static int ParticipationPoints(IEnumerable<ProgressAttempt> attempts) => attempts
        .SelectMany(item => new[]
        {
            (item.ContentId, Day: DateOnly.FromDateTime(item.AnsweredAtUtc.UtcDateTime),
                Points: item.WasGuessed ? 1 : 2 + (item.IsCorrect ? 1 : 0), Explanation: false),
            item.ExplanationViewedAtUtc.HasValue ?
                (item.ContentId, Day: DateOnly.FromDateTime(item.ExplanationViewedAtUtc.Value.UtcDateTime),
                    Points: 0, Explanation: true) :
                ((Guid ContentId, DateOnly Day, int Points, bool Explanation)?)null
        }.OfType<(Guid ContentId, DateOnly Day, int Points, bool Explanation)>())
        .GroupBy(item => (item.ContentId, item.Day))
        .Sum(group => Math.Min(4, group.Max(item => item.Points) +
            (group.Any(item => item.Explanation) ? 1 : 0)));

    public static bool HasImproved(IEnumerable<ProgressAttempt> attempts)
    {
        var uncertain = false;
        var events = attempts.SelectMany(item => new[]
        {
            (AtUtc: item.AnsweredAtUtc, IsAnswer: true,
                IsSafe: item.IsCorrect && !item.WasGuessed),
            item.ExplanationViewedAtUtc.HasValue ?
                (AtUtc: item.ExplanationViewedAtUtc.Value, IsAnswer: false, IsSafe: false) :
                ((DateTimeOffset AtUtc, bool IsAnswer, bool IsSafe)?)null
        }.OfType<(DateTimeOffset AtUtc, bool IsAnswer, bool IsSafe)>())
            .OrderBy(item => item.AtUtc).ThenBy(item => item.IsAnswer ? 0 : 1);
        foreach (var item in events)
        {
            if (uncertain && item.IsAnswer && item.IsSafe) return true;
            if (!item.IsSafe) uncertain = true;
        }
        return false;
    }
}
