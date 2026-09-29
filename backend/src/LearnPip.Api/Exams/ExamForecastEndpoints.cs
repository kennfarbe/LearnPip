using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public sealed record ForecastEvidence(int AnsweredCatalogQuestions, int CatalogQuestions,
    int SpacedMasteredContents, int OwnContents, int CompletedSimulations,
    int RecentSimulations, int RecentPassedSimulations);
public sealed record ForecastSession(DateOnly Date, string Place, DateOnly? RegistrationDeadline,
    string RegistrationStatus, string SourceUrl, DateOnly CheckedOn);
public sealed record ExamForecast(string Status, DateOnly? EarliestReadyDate,
    DateOnly? LatestReadyDate, DateOnly? SuggestedExamDate, ForecastEvidence Evidence,
    IReadOnlyList<string> Reasons, string Assumptions, string FormalAdmissionStatus,
    string? RulesSourceUrl, DateOnly? RulesCheckedOn, int ProfileVersion,
    IReadOnlyList<ForecastSession> Sessions);
public sealed record SimulationSignal(DateTimeOffset CompletedAtUtc, bool Passed);

public static class ExamForecastEstimator
{
    public static ExamForecast Estimate(ForecastEvidence evidence,
        IReadOnlyList<SimulationSignal> simulations, IReadOnlyList<ExamSession> sessions,
        int version, string? rulesSource, DateOnly? rulesChecked, DateOnly today)
    {
        var recent = simulations.Where(item =>
                DateOnly.FromDateTime(item.CompletedAtUtc.UtcDateTime) >= today.AddDays(-30))
            .OrderByDescending(item => item.CompletedAtUtc).Take(2).ToArray();
        var reasons = new List<string>();
        if (evidence.CatalogQuestions == 0 ||
            evidence.AnsweredCatalogQuestions * 100 < evidence.CatalogQuestions * 80)
            reasons.Add("Weniger als 80 % der Original-Fragegruppen wurden beantwortet.");
        if (evidence.OwnContents < 3 ||
            evidence.SpacedMasteredContents * 100 < evidence.OwnContents * 70)
            reasons.Add("Zu wenige eigene Lerninhalte haben drei zeitversetzte sichere Wiederholungen.");
        if (recent.Length < 2 || recent.Any(item => !item.Passed))
            reasons.Add("Es fehlen zwei vollständig bestandene Simulationen ohne angerechnete Teile aus den letzten 30 Tagen.");
        var earliest = reasons.Count == 0 ? today.AddDays(7) : (DateOnly?)null;
        var latest = reasons.Count == 0 ? today.AddDays(28) : (DateOnly?)null;
        var visible = sessions.OrderBy(item => item.Date).ThenBy(item => item.Place)
            .Select(item => new ForecastSession(item.Date, item.Place, item.RegistrationDeadline,
                item.RegistrationDeadline is null ? "unknown" :
                    item.RegistrationDeadline < today ? "closed" : "open",
                item.SourceUrl, item.CheckedOn)).ToArray();
        var suggestion = earliest is null ? null : visible.FirstOrDefault(item =>
            item.Date >= earliest && item.RegistrationStatus == "open" &&
            item.CheckedOn >= today.AddDays(-30) && rulesChecked >= today.AddDays(-30))?.Date;
        if (sessions.Count == 0) reasons.Add("Keine geprüften Termine in dieser Profilfassung.");
        else if (suggestion is null && earliest != null)
            reasons.Add("Kein Termin mit offener, kürzlich geprüfter Anmeldefrist verfügbar.");
        return new ExamForecast(earliest is null ? "insufficient" : "window", earliest,
            latest, suggestion, evidence, reasons,
            "Heuristik, keine Erfolgswahrscheinlichkeit: mindestens 80 % beantwortete " +
            "Original-Fragegruppen, mindestens 70 % eigene Inhalte mit drei zeitversetzten " +
            "sicheren Wiederholungen (mindestens drei Inhalte) und die letzten zwei " +
            "Simulationen derselben Profilfassung ohne angerechnete Teile in 30 Tagen " +
            "bestanden. Eigene Inhalte " +
            "sind nicht zuverlässig dem amtlichen Katalog zugeordnet. Das Fenster " +
            "7–28 Tage enthält einen Vorbereitungspuffer; weitere Übung und geänderte " +
            "Regeln können es verschieben. Termine werden nur bei Quellenstand bis " +
            "30 Tage und bekannter offener Frist vorgeschlagen.",
            "Nicht geprüft: formale Zulassung und tatsächliche Anmeldung müssen " +
            "bei der Prüfungsstelle separat geklärt werden.", rulesSource, rulesChecked,
            version, visible);
    }
}

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
