// <copyright file="LearningSessionEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.Ai;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed record StartLearningRequest(Guid? CatalogId, int Count = 5, string Language = "de");
public sealed record LearningAnswerRequest(IReadOnlyList<Guid> SelectedOptionIds, bool WasGuessed = false);
public sealed record ExplanationViewRequest(Guid AttemptId);
public sealed record LearningOption(Guid Id, IReadOnlyList<ContentBlockOutput> Blocks);
public sealed record LearningQuestion(Guid QuestionId, Guid VersionId, string SelectionMode,
    IReadOnlyList<ContentBlockOutput> Prompt, IReadOnlyList<LearningOption> Answers,
    string? Hint, string? NextStep, string Language, string RequestedLanguage,
    bool TranslationMissing, Guid? TranslationId, int VersionNumber);
public sealed record LearningSessionView(Guid Id, int Total, int Answered, int Skipped,
    bool Completed, LearningQuestion? Current);
public sealed record LearningFeedback(Guid AttemptId, Guid ContentId, bool IsCorrect,
    IReadOnlyList<Guid> CorrectOptionIds,
    IReadOnlyList<ContentBlockOutput> Explanation, string? ShortExplanation);

internal sealed record LearningPlanItem(Guid QuestionId, Guid VersionId, Guid[] OptionIds,
    string State, string Language = "de", Guid? TranslationId = null);

public static class LearningSessionEndpoints
{
    public static IEndpointRouteBuilder MapLearningSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var sessions = app.MapGroup("/api/v1/learning/sessions").WithTags("Learning")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        sessions.MapPost("/", Start);
        sessions.MapGet("/{id:guid}", Read);
        sessions.MapPost("/{id:guid}/answer", Answer);
        sessions.MapPost("/{id:guid}/skip", Skip);
        sessions.MapPost("/{id:guid}/explanation", ExplanationViewed);
        return app;
    }

    private static async Task<IResult> Start(StartLearningRequest request, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (request.Count is < 1 or > 10 || !TranslationEndpoints.ValidLanguage(request.Language))
            return Results.BadRequest();
        if (request.CatalogId.HasValue && !await db.PrivateCatalogs.AnyAsync(item =>
                item.Id == request.CatalogId && item.OwnerAccountId == accountId, cancellationToken))
            return Results.NotFound();

        // One variant per learning content. Randomness only breaks scheduling ties and shuffles answers.
        var (overview, available) = await ReviewEndpoints.LoadWithCandidates(db, accountId,
            cancellationToken);
        var candidates = available.Where(item => !request.CatalogId.HasValue ||
            item.CatalogId == request.CatalogId).GroupBy(item => item.ContentId)
            .Select(group => group.ToList()).ToList();
        Shuffle(candidates);
        var now = DateTimeOffset.UtcNow;
        candidates = candidates.OrderBy(group =>
                overview.Contents.Single(item => item.Id == group[0].ContentId).DueAtUtc > now ? 1 : 0)
            .ThenByDescending(group => overview.Contents.Single(item =>
                item.Id == group[0].ContentId).OftenForMe)
            .ThenBy(group => overview.Contents.Single(item =>
                item.Id == group[0].ContentId).DueAtUtc ?? DateTimeOffset.MinValue)
            .ToList();
        var plan = new List<LearningPlanItem>();
        foreach (var variants in candidates.Take(request.Count))
        {
            var candidate = variants[RandomNumberGenerator.GetInt32(variants.Count)];
            var optionIds = await db.AnswerOptions.AsNoTracking()
                .Where(option => option.QuestionVersionId == candidate.VersionId)
                .Select(option => option.Id).ToListAsync(cancellationToken);
            Shuffle(optionIds);
            var baseLanguage = await db.QuestionVersions.Where(version =>
                version.Id == candidate.VersionId).Select(version => version.Language)
                .SingleAsync(cancellationToken);
            var translationId = baseLanguage == request.Language ? null :
                await db.QuestionTranslations.AsNoTracking().Where(item =>
                    item.QuestionVersionId == candidate.VersionId &&
                    item.Language == request.Language && item.Status == "approved")
                    .OrderByDescending(item => item.Revision).Select(item => (Guid?)item.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            plan.Add(new LearningPlanItem(candidate.QuestionId, candidate.VersionId,
                optionIds.ToArray(), "pending", request.Language, translationId));
        }
        if (plan.Count == 0) return Results.Conflict(new { message = "No published questions available." });
        var session = new StudySession { AccountId = accountId, PlanJson = JsonSerializer.Serialize(plan) };
        db.StudySessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/learning/sessions/{session.Id}",
            new ApiResponse<LearningSessionView>((await View(session, plan, db, cancellationToken))!));
    }

    private static async Task<IResult> Read(Guid id, LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var session = await db.StudySessions.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == id && item.AccountId == accountId && item.PlanJson != null, cancellationToken);
        if (session == null) return Results.NotFound();
        return Results.Ok(new ApiResponse<LearningSessionView>((await View(session,
            Parse(session), db, cancellationToken))!));
    }

    private static async Task<IResult> Answer(Guid id, LearningAnswerRequest request,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await LockedSession(id, accountId, db, cancellationToken);
        if (session == null) return Results.NotFound();
        var plan = Parse(session);
        var current = plan.FirstOrDefault(item => item.State == "pending");
        if (current == null) return Results.Conflict();
        var selected = request.SelectedOptionIds;
        var version = await QuestionEndpoints.LoadVersion(db, current.VersionId, cancellationToken);
        if (version == null) return Results.NotFound();
        if (selected == null || selected.Count == 0 || selected.Count != selected.Distinct().Count() ||
            version.SelectionMode == "single" && selected.Count != 1 ||
            selected.Any(optionId => !current.OptionIds.Contains(optionId)))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["selectedOptionIds"] = ["Select distinct options from this question; single choice requires one."]
            });
        var correct = version.Answers.Where(option => option.IsCorrect)
            .Select(option => option.Id).ToArray();
        var isCorrect = selected.Count == correct.Length && selected.All(correct.Contains);
        var attempt = new StudyAttempt
        {
            StudySessionId = session.Id,
            QuestionVersionId = current.VersionId,
            IsCorrect = isCorrect,
            WasGuessed = request.WasGuessed
        };
        db.StudyAttempts.Add(attempt);
        foreach (var optionId in selected)
            db.StudyAttemptSelections.Add(new StudyAttemptSelection
            {
                StudyAttemptId = attempt.Id,
                AnswerOptionId = optionId
            });
        CompleteItem(session, plan, current, "answered");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        version = await TranslationEndpoints.Localize(version, current.TranslationId, db,
            cancellationToken);
        var shortText = version.Explanation.FirstOrDefault(block => block.Kind == "text")?.Text;
        var shortExplanation = shortText is { Length: > 160 } ? shortText[..160] + "…" : shortText;
        var contentId = await db.Questions.Where(question => question.Id == current.QuestionId)
            .Select(question => question.LearningContentId ?? question.Id)
            .SingleAsync(cancellationToken);
        return Results.Ok(new ApiResponse<LearningFeedback>(new LearningFeedback(attempt.Id,
            contentId, isCorrect, correct, version.Explanation, shortExplanation)));
    }

    private static async Task<IResult> ExplanationViewed(Guid id, ExplanationViewRequest request,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var attempt = await db.StudyAttempts.SingleOrDefaultAsync(item => item.Id == request.AttemptId &&
            item.StudySessionId == id && item.StudySession.AccountId == accountId &&
            item.QuestionVersion.Explanation != null, cancellationToken);
        if (attempt == null) return Results.NotFound();
        if (attempt.ExplanationViewedAtUtc == null)
        {
            attempt.ExplanationViewedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> Skip(Guid id, LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await LockedSession(id, accountId, db, cancellationToken);
        if (session == null) return Results.NotFound();
        var plan = Parse(session);
        var current = plan.FirstOrDefault(item => item.State == "pending");
        if (current == null) return Results.Conflict();
        CompleteItem(session, plan, current, "skipped");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<StudySession?> LockedSession(Guid id, Guid accountId, LearnPipDbContext db,
        CancellationToken cancellationToken)
    {
        var session = await db.StudySessions.FromSqlInterpolated(
            $"SELECT * FROM \"StudySessions\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        return session?.AccountId == accountId && session.PlanJson != null ? session : null;
    }

    private static void CompleteItem(StudySession session, List<LearningPlanItem> plan,
        LearningPlanItem current, string state)
    {
        plan[plan.IndexOf(current)] = current with { State = state };
        session.PlanJson = JsonSerializer.Serialize(plan);
        if (plan.All(item => item.State != "pending")) session.CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    private static List<LearningPlanItem> Parse(StudySession session) =>
        JsonSerializer.Deserialize<List<LearningPlanItem>>(session.PlanJson!)!;

    private static async Task<LearningSessionView> View(StudySession session,
        List<LearningPlanItem> plan, LearnPipDbContext db, CancellationToken cancellationToken)
    {
        var current = plan.FirstOrDefault(item => item.State == "pending");
        LearningQuestion? question = null;
        if (current != null)
        {
            var version = await QuestionEndpoints.LoadVersion(db, current.VersionId, cancellationToken);
            if (version != null)
            {
                version = await TranslationEndpoints.Localize(version, current.TranslationId, db,
                    cancellationToken);
                var guidance = Guidance(version.Explanation, version.Answers);
                question = new LearningQuestion(current.QuestionId, current.VersionId,
                    version.SelectionMode, version.Prompt, current.OptionIds.Select(id =>
                    {
                        var option = version.Answers.Single(answer => answer.Id == id);
                        return new LearningOption(id, option.Blocks);
                    }).ToArray(), guidance.Hint, guidance.NextStep,
                    version.Language, current.Language, version.Language != current.Language,
                    current.TranslationId, version.Version);
            }
        }
        return new LearningSessionView(session.Id, plan.Count,
            plan.Count(item => item.State == "answered"), plan.Count(item => item.State == "skipped"),
            session.CompletedAtUtc != null, question);
    }

    public static (string? Hint, string? NextStep) Guidance(
        IReadOnlyList<ContentBlockOutput> explanation, IReadOnlyList<AnswerOutput> answers)
    {
        var text = explanation.FirstOrDefault(block => block.Kind == "text")?.Text ?? string.Empty;
        var lines = text.Split('\n');
        string? Find(string marker) => lines.FirstOrDefault(line => line.StartsWith(marker,
            StringComparison.Ordinal))?[marker.Length..].Trim();
        var correct = answers.Where(answer => answer.IsCorrect)
            .SelectMany(answer => answer.Blocks).Where(block => block.Kind == "text")
            .Select(block => block.Text ?? string.Empty).ToArray();
        string? Safe(string? value) => value is { Length: > 0 and <= 500 } &&
            correct.All(answer => SolutionVerifier.SafeHint(value, answer)) ? value : null;
        return (Safe(Find("[Hinweis] ")), Safe(Find("[Nächster Schritt] ")));
    }

    private static void Shuffle<T>(IList<T> items)
    {
        for (var index = items.Count - 1; index > 0; index--)
        {
            var other = RandomNumberGenerator.GetInt32(index + 1);
            (items[index], items[other]) = (items[other], items[index]);
        }
    }
}
