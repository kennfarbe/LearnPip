// <copyright file="ReviewEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        var learning = app.MapGroup("/api/v1/learning").WithTags("Adaptive review")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        learning.MapGet("/review", Read);
        learning.MapPut("/contents/{id:guid}/often-for-me", AddFrequent);
        learning.MapDelete("/contents/{id:guid}/often-for-me", RemoveFrequent);
        learning.MapPut("/questions/{id:guid}/content", AssignContent);
        return app;
    }

    private static async Task<IResult> Read(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var overview = await Load(db, accountId, cancellationToken);
        return Results.Ok(new ApiResponse<ReviewOverview>(overview));
    }

    private static async Task<IResult> AddFrequent(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!await db.LearningContents.AnyAsync(item => item.Id == id &&
                item.OwnerAccountId == accountId, cancellationToken)) return Results.NotFound();
        if (!await db.FrequentLearningContents.AnyAsync(item => item.AccountId == accountId &&
                item.LearningContentId == id, cancellationToken))
        {
            db.FrequentLearningContents.Add(new FrequentLearningContent
            {
                AccountId = accountId,
                LearningContentId = id
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveFrequent(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var item = await db.FrequentLearningContents.SingleOrDefaultAsync(row =>
            row.LearningContentId == id && row.AccountId == accountId, cancellationToken);
        if (item == null) return Results.NotFound();
        db.FrequentLearningContents.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AssignContent(Guid id, ContentAssignment request,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var question = await db.Questions.SingleOrDefaultAsync(item => item.Id == id &&
            item.OwnerAccountId == accountId && item.DeletedAtUtc == null, cancellationToken);
        if (question == null || !await db.LearningContents.AnyAsync(item =>
                item.Id == request.ContentId && item.OwnerAccountId == accountId,
                cancellationToken)) return Results.NotFound();
        question.LearningContentId = request.ContentId;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    internal static async Task<(ReviewOverview Overview, List<ReviewCandidate> Candidates)> LoadWithCandidates(
        LearnPipDbContext db, Guid accountId, CancellationToken cancellationToken)
    {
        var candidates = await db.Questions.AsNoTracking()
            .Where(question => question.OwnerAccountId == accountId && question.DeletedAtUtc == null)
            .Select(question => new
            {
                question.Id,
                question.PrivateCatalogId,
                ContentId = question.LearningContentId ?? question.Id,
                Title = question.LearningContent != null ? question.LearningContent.Title :
                    question.Versions.OrderByDescending(v => v.VersionNumber)
                        .Select(v => v.Topic).FirstOrDefault() ?? "Lerninhalt",
                Subject = question.Versions.OrderByDescending(v => v.VersionNumber)
                    .Select(v => v.Subject).FirstOrDefault() ?? "Allgemein",
                VersionId = question.Versions.OrderByDescending(v => v.VersionNumber)
                    .Select(v => (Guid?)v.Id).FirstOrDefault()
            })
            .Where(item => item.VersionId != null)
            .ToListAsync(cancellationToken);
        var items = candidates.Select(item => new ReviewCandidate(item.Id, item.VersionId!.Value,
            item.ContentId, item.Title, item.Subject, item.PrivateCatalogId)).ToList();
        var attempts = await db.StudyAttempts.AsNoTracking()
            .Where(item => item.StudySession.AccountId == accountId &&
                item.QuestionVersion.Question.OwnerAccountId == accountId)
            .Select(item => new
            {
                item.Id,
                item.AnsweredAtUtc,
                item.IsCorrect,
                item.WasGuessed,
                item.ExplanationViewedAtUtc,
                ContentId = item.QuestionVersion.Question.LearningContentId ??
                    item.QuestionVersion.QuestionId
            }).ToListAsync(cancellationToken);
        var events = attempts.GroupBy(item => item.ContentId).ToDictionary(group => group.Key,
            group => group.SelectMany(item => new[]
            {
                new ReviewEvent(item.Id, item.AnsweredAtUtc, "answer", item.IsCorrect),
                item.WasGuessed ? new ReviewEvent(item.Id, item.AnsweredAtUtc, "guess", false) : null,
                item.ExplanationViewedAtUtc.HasValue ?
                    new ReviewEvent(item.Id, item.ExplanationViewedAtUtc.Value, "explanation", false) : null
            }.OfType<ReviewEvent>()).ToList());
        var frequent = (await db.FrequentLearningContents.AsNoTracking()
            .Where(item => item.AccountId == accountId).Select(item => item.LearningContentId)
            .ToListAsync(cancellationToken)).ToHashSet();
        var contents = items.GroupBy(item => item.ContentId).Select(group =>
        {
            var state = ReviewSchedule.Replay(events.GetValueOrDefault(group.Key) ?? []);
            return new LearningContentView(group.Key, group.First().Title,
                group.Select(item => item.QuestionId).ToArray(), frequent.Contains(group.Key),
                state.ConfidentStreak, state.DueAtUtc, state.Mastered, state.Answers,
                state.Guesses, state.ExplanationsViewed);
        }).OrderBy(item => item.Title).ToArray();
        return (new ReviewOverview(contents.Length, contents.Count(item => item.Mastered),
            contents.Count(item => item.OftenForMe), contents), items);
    }

    private static async Task<ReviewOverview> Load(LearnPipDbContext db, Guid accountId,
        CancellationToken cancellationToken) =>
        (await LoadWithCandidates(db, accountId, cancellationToken)).Overview;
}
