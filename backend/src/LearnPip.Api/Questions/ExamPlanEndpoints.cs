// <copyright file="ExamPlanEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Registriert HTTP-Endpunkte für die Planung des Lernumfangs vor einer Prüfung.
/// </summary>
public static class ExamPlanEndpoints
{
    /// <summary>
    /// Registriert HTTP-Endpunkte für die Planung des Lernumfangs vor einer Prüfung.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapExamPlanEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/api/v1/learning/exam-plan/estimate",
            Estimate)
            .WithTags("Exam planning").RequireAuthorization(ApiPolicies.ActiveAccount);
        return app;
    }

    private static async Task<IResult> Estimate(
        ExamPlanInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = input.ExamDate.HasValue ? input.ExamDate.Value.DayNumber - start.DayNumber :
            input.HorizonDays ?? 28;
        var school = input.SchoolDays ?? [];
        var breaks = input.BreakDays ?? [];
        var missing = (input.ScopeContents == null ? 1 : 0) +
            (input.DailyMinutes == null ? 1 : 0) + (input.TargetPercent == null ? 1 : 0);
        if (missing != 1 || input.ScopeContents is < 1 or > 2000 ||
            input.DailyMinutes is < 1 or > 480 || input.TargetPercent is < 1 or > 100 ||
            input.DailyLimitMinutes is < 5 or > 480 || days is < 1 or > 365 ||
            school.Count > 7 || school.Any(day => day is < 1 or > 7) ||
            school.Count != school.Distinct().Count() || breaks.Count > 365 ||
            breaks.Count != breaks.Distinct().Count() ||
            breaks.Any(day => day < start || day.DayNumber >= start.DayNumber + days) ||
            input.ContentIds is { Count: > 2000 } ||
            (input.ContentIds != null && input.ContentIds.Count != input.ContentIds.Distinct().Count()))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["plan"] = ["Provide two triangle values and valid dates, limits and unique selections."],
            });
        }

        var (overview, _) = await ReviewEndpoints.LoadWithCandidates(
            db,
            accountId,
            cancellationToken);
        var contents = overview.Contents;
        if (input.ContentIds != null)
        {
            var selected = input.ContentIds.ToHashSet();
            contents = contents.Where(item => selected.Contains(item.Id)).ToArray();
            if (contents.Count != selected.Count)
            {
                return Results.NotFound();
            }
        }

        if (contents.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["contentIds"] = ["Create a learning content before estimating a plan."],
            });
        }

        if (input.ScopeContents.HasValue && input.ScopeContents.Value > contents.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["scopeContents"] = ["The selected scope exceeds the available learning contents."],
            });
        }

        var included = contents.Take(input.ScopeContents ?? contents.Count)
            .Select(item => item.Id).ToHashSet();
        var raw = await db.StudyAttempts.AsNoTracking()
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
                    item.QuestionVersion.QuestionId,
            }).ToListAsync(cancellationToken);
        var mastered = raw.Where(item => included.Contains(item.ContentId))
            .GroupBy(item => item.ContentId)
            .Count(group => ExamPlanEstimator.HasSpacedMastery(group.SelectMany(item => new[]
            {
                new ReviewEvent(item.Id, item.AnsweredAtUtc, "answer", item.IsCorrect),
                item.WasGuessed ? new ReviewEvent(item.Id, item.AnsweredAtUtc, "guess", false) : null,
                item.ExplanationViewedAtUtc.HasValue ?
                    new ReviewEvent(item.Id, item.ExplanationViewedAtUtc.Value, "explanation", false) : null,
            }.OfType<ReviewEvent>())));
        return Results.Ok(new ApiResponse<ExamPlanResult>(ExamPlanEstimator.Estimate(
                    input,
                    contents.Count,
                    mastered,
                    start)));
    }
}
