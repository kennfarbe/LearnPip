// <copyright file="ExamForecastEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public static class ExamForecastEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<IResult> Read(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var profile = await db.ExamProfileVersions.AsNoTracking()
            .Include(item => item.CatalogEdition)
            .SingleOrDefaultAsync(item => item.Id == id, ct);
        if (profile == null) return Results.NotFound();
        var parts = JsonSerializer.Deserialize<List<ProfilePart>>(profile.PartsJson, Json)!;
        var partCodes = parts.Select(item => item.CatalogPartCode).ToHashSet();
        var originals = JsonSerializer.Deserialize<List<CatalogQuestion>>(
            profile.CatalogEdition.QuestionsJson, Json)!
            .Where(item => item.Kind == "original" && partCodes.Contains(item.PartCode))
            .Select(item => item.Code).ToHashSet();
        var runs = await db.ExamSimulations.AsNoTracking().Where(item =>
            item.AccountId == accountId && item.ProfileVersionId == id).ToListAsync(ct);
        var answered = new HashSet<string>();
        var signals = new List<SimulationSignal>();
        foreach (var run in runs)
        {
            var answers = JsonSerializer.Deserialize<Dictionary<string, int>>(run.AnswersJson, Json)!;
            if (run.SnapshotJson.StartsWith('['))
            {
                var snapshot = JsonSerializer.Deserialize<List<SnapshotPart>>(run.SnapshotJson, Json)!;
                foreach (var question in snapshot.SelectMany(part => part.Questions))
                    if (answers.ContainsKey(question.Code))
                        answered.Add(question.BaseCode ?? question.Code);
                if (run.CompletedAtUtc != null)
                {
                    var results = JsonSerializer.Deserialize<List<PartResult>>(run.ResultJson!, Json)!;
                    signals.Add(new SimulationSignal(run.CompletedAtUtc.Value,
                        snapshot.All(part => !part.Credited) &&
                        ExamScoring.Passed(snapshot, results)));
                }
            }
            else
            {
                var snapshot = JsonSerializer.Deserialize<PowerSnapshot>(run.SnapshotJson, Json)!;
                foreach (var question in snapshot.Parts.SelectMany(part => part.Questions))
                    if (answers.ContainsKey(question.Code))
                        answered.Add(question.BaseCode ?? question.Code);
            }
        }
        var (overview, _) = await ReviewEndpoints.LoadWithCandidates(db, accountId, ct);
        var contentIds = overview.Contents.Select(item => item.Id).ToHashSet();
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
                    item.QuestionVersion.QuestionId
            }).ToListAsync(ct);
        var mastered = raw.Where(item => contentIds.Contains(item.ContentId))
            .GroupBy(item => item.ContentId).Count(group =>
                ExamPlanEstimator.HasSpacedMastery(group.SelectMany(item => new[]
                {
                    new ReviewEvent(item.Id, item.AnsweredAtUtc, "answer", item.IsCorrect),
                    item.WasGuessed ? new ReviewEvent(item.Id, item.AnsweredAtUtc, "guess", false) : null,
                    item.ExplanationViewedAtUtc.HasValue ? new ReviewEvent(item.Id,
                        item.ExplanationViewedAtUtc.Value, "explanation", false) : null
                }.OfType<ReviewEvent>())));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var evidence = new ForecastEvidence(answered.Count(originals.Contains), originals.Count,
            mastered, contentIds.Count, signals.Count, signals.Count(item =>
                DateOnly.FromDateTime(item.CompletedAtUtc.UtcDateTime) >= today.AddDays(-30)),
            signals.Count(item => item.Passed &&
                DateOnly.FromDateTime(item.CompletedAtUtc.UtcDateTime) >= today.AddDays(-30)));
        var sessions = JsonSerializer.Deserialize<List<ExamSession>>(profile.ScheduleJson, Json)!;
        return Results.Ok(new ApiResponse<ExamForecast>(ExamForecastEstimator.Estimate(evidence,
            signals, sessions, profile.Version, profile.RulesSourceUrl,
            profile.RulesCheckedOn, today)));
    }
}
