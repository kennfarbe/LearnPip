// <copyright file="ProgressMetrics.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Api.Questions;

/// <summary>
/// Berechnet Teilnahme und langfristigen Lernfortschritt aus Antwortversuchen.
/// </summary>
public static class ProgressMetrics
{
    // A day/content pair earns at most four points. Repeating clicks on the same
    // question or variant does not accumulate points; time spent is irrelevant.

    /// <summary>
    /// Berechnet Teilnahmepunkte aus Antwortversuchen und angesehenen Erklärungen.
    /// </summary>
    /// <param name="attempts">Die für den Lernfortschritt berücksichtigten Antwortversuche.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static int ParticipationPoints(IEnumerable<ProgressAttempt> attempts) => attempts
        .SelectMany(item => new[]
        {
            (item.ContentId, Day: DateOnly.FromDateTime(item.AnsweredAtUtc.UtcDateTime),
                Points: PointsFor(item), Explanation: false),
            item.ExplanationViewedAtUtc.HasValue ?
                (item.ContentId, Day: DateOnly.FromDateTime(item.ExplanationViewedAtUtc.Value.UtcDateTime),
                    Points: 0, Explanation: true) :
                ((Guid ContentId, DateOnly Day, int Points, bool Explanation)?)null,
        }.OfType<(Guid ContentId, DateOnly Day, int Points, bool Explanation)>())
        .GroupBy(item => (item.ContentId, item.Day))
        .Sum(group => Math.Min(4, group.Max(item => item.Points) + (group.Any(item => item.Explanation) ? 1 : 0)));

    /// <summary>
    /// Prüft, ob sich der Lernstand zwischen frühen und späteren Antwortversuchen verbessert hat.
    /// </summary>
    /// <param name="attempts">Die für den Lernfortschritt berücksichtigten Antwortversuche.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool HasImproved(IEnumerable<ProgressAttempt> attempts)
    {
        var uncertain = false;
        var events = attempts.SelectMany(item => new[]
        {
            (AtUtc: item.AnsweredAtUtc, IsAnswer: true,
                IsSafe: item.IsCorrect && !item.WasGuessed),
            item.ExplanationViewedAtUtc.HasValue ?
                (AtUtc: item.ExplanationViewedAtUtc.Value, IsAnswer: false, IsSafe: false) :
                ((DateTimeOffset AtUtc, bool IsAnswer, bool IsSafe)?)null,
        }.OfType<(DateTimeOffset AtUtc, bool IsAnswer, bool IsSafe)>())
            .OrderBy(item => item.AtUtc).ThenBy(item => item.IsAnswer ? 0 : 1);
        foreach (var item in events)
        {
            if (uncertain && item.IsAnswer && item.IsSafe)
            {
                return true;
            }

            if (!item.IsSafe)
            {
                uncertain = true;
            }
        }

        return false;
    }

    private static int PointsFor(ProgressAttempt item)
    {
        if (item.WasGuessed)
        {
            return 1;
        }

        return item.IsCorrect ? 3 : 2;
    }
}
