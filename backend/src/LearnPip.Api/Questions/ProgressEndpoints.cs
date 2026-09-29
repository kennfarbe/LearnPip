using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed record TopicProgress(string Subject, string Topic, int TotalContents,
    int MasteredContents, int ImprovedContents);
public sealed record ProgressWeek(string Label, int CompletedSessions, int ActiveDays);
public sealed record LearningProgress(int TotalContents, int MasteredContents,
    int ImprovedContents, int ParticipationPoints, int LearningDays,
    IReadOnlyList<TopicProgress> Topics, IReadOnlyList<ProgressWeek> RecentWeeks);

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/learning/progress", Read)
            .WithTags("Learning progress").RequireAuthorization(ApiPolicies.ActiveAccount);
        return app;
    }

    private static async Task<IResult> Read(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var (overview, candidates) = await ReviewEndpoints.LoadWithCandidates(db, accountId,
            cancellationToken);
        var raw = await db.StudyAttempts.AsNoTracking()
            .Where(item => item.StudySession.AccountId == accountId &&
                item.QuestionVersion.Question.OwnerAccountId == accountId)
            .Select(item => new
            {
                item.StudySessionId,
                item.AnsweredAtUtc,
                item.IsCorrect,
                item.WasGuessed,
                item.ExplanationViewedAtUtc,
                ContentId = item.QuestionVersion.Question.LearningContentId ??
                    item.QuestionVersion.QuestionId
            }).ToListAsync(cancellationToken);
        var attempts = raw.Select(item => new ProgressAttempt(item.ContentId, item.StudySessionId,
            item.AnsweredAtUtc, item.IsCorrect, item.WasGuessed, item.ExplanationViewedAtUtc)).ToArray();
        var improved = attempts.GroupBy(item => item.ContentId)
            .Where(group => ProgressMetrics.HasImproved(group))
            .Select(group => group.Key).ToHashSet();
        var topics = overview.Contents.Select(content =>
        {
            var candidate = candidates.First(item => item.ContentId == content.Id);
            return new { candidate.Subject, Topic = content.Title, Content = content };
        }).GroupBy(item => (item.Subject, item.Topic))
            .Select(group => new TopicProgress(group.Key.Subject, group.Key.Topic, group.Count(),
                group.Count(item => item.Content.Mastered),
                group.Count(item => improved.Contains(item.Content.Id))))
            .OrderBy(item => item.Subject).ThenBy(item => item.Topic).ToArray();

        var completed = await db.StudySessions.AsNoTracking()
            .Where(item => item.AccountId == accountId && item.PlanJson != null &&
                item.CompletedAtUtc != null && item.Attempts.Any())
            .Select(item => new { item.Id, item.CompletedAtUtc }).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var weeks = Enumerable.Range(0, 4).Select(index =>
        {
            var end = now.AddDays(-7 * index);
            var start = end.AddDays(-7);
            var sessions = completed.Count(item => item.CompletedAtUtc > start &&
                item.CompletedAtUtc <= end);
            var days = attempts.Where(item => item.AnsweredAtUtc > start &&
                    item.AnsweredAtUtc <= end)
                .Select(item => DateOnly.FromDateTime(item.AnsweredAtUtc.UtcDateTime))
                .Distinct().Count();
            return new ProgressWeek(index == 0 ? "Letzte 7 Tage" : $"Vor {index} Woche(n)",
                sessions, days);
        }).Reverse().ToArray();
        var result = new LearningProgress(overview.TotalContents, overview.MasteredContents,
            overview.Contents.Count(item => improved.Contains(item.Id)),
            ProgressMetrics.ParticipationPoints(attempts),
            attempts.Select(item => DateOnly.FromDateTime(item.AnsweredAtUtc.UtcDateTime))
                .Distinct().Count(), topics, weeks);
        return Results.Ok(new ApiResponse<LearningProgress>(result));
    }
}
