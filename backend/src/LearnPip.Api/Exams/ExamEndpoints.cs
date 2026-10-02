// <copyright file="ExamEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public sealed record CatalogQuestion(string Code, string PartCode, string Prompt,
    IReadOnlyList<string> Answers, int CorrectIndex, string Kind = "original",
    string? BaseCode = null);
public sealed record CatalogImportInput(string Code, string Title, string Revision,
    string SourceUrl, string License, string Attribution, DateOnly ChangedOn,
    bool RightsConfirmed, IReadOnlyList<CatalogQuestion> Questions);
public sealed record ProfilePart(string Code, string Title, string CatalogPartCode,
    int QuestionCount, int TimeLimitMinutes, int RequiredCorrect, string? CreditCode,
    bool AllowVariants = false, bool ShuffleAnswers = true);
public sealed record ProfileInput(string Code, string Title, string AmateurClass,
    Guid CatalogEditionId, IReadOnlyList<ProfilePart> Parts,
    IReadOnlyList<ExamSession>? Sessions = null, string? RulesSourceUrl = null,
    DateOnly? RulesCheckedOn = null);
public sealed record ExamSession(DateOnly Date, string Place, DateOnly? RegistrationDeadline,
    string SourceUrl, DateOnly CheckedOn);
public sealed record StartSimulationInput(Guid ProfileVersionId, string QuestionMode = "original");
public sealed record StartPowerTestInput(Guid ProfileVersionId, string QuestionMode = "original",
    int StageSize = 25);
public sealed record AnswerSimulationInput(int SelectedIndex);
public sealed record CreditInput(string Code);
public sealed record SnapshotPart(ProfilePart Rule, bool Credited,
    IReadOnlyList<CatalogQuestion> Questions);
public sealed record PartResult(string Code, int Correct, int Total, bool Passed,
    bool Credited, bool TimedOut);
public sealed record SimulationView(Guid Id, Guid ProfileVersionId, int ProfileVersion,
    string ProfileCode, string CatalogRevision, DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? CurrentPartCode, DateTimeOffset? DeadlineAtUtc,
    IReadOnlyList<object> Questions, IReadOnlyList<PartResult> Parts, bool? Passed,
    IReadOnlyDictionary<string, int> SelectedAnswers);
public sealed record PowerSnapshot(string QuestionMode, int StageSize,
    IReadOnlyList<SnapshotPart> Parts);
public sealed record PowerMistake(string Code, string PartCode, string Prompt,
    string? SelectedAnswer, string CorrectAnswer);
public sealed record PowerPartResult(string Code, int Correct, int Total);
public sealed record PowerView(Guid Id, Guid ProfileVersionId, string ProfileCode,
    string CatalogRevision, string QuestionMode, int Stage, int Stages,
    DateTimeOffset? CompletedAtUtc, IReadOnlyList<object> Questions,
    IReadOnlyDictionary<string, int> SelectedAnswers,
    IReadOnlyList<PowerPartResult> Parts, IReadOnlyList<PowerMistake> Mistakes);

public static class ExamQuestionSelection
{
    public static IReadOnlyList<CatalogQuestion> Select(IReadOnlyList<CatalogQuestion> catalog,
        ProfilePart rule, string mode, int? count, Random random)
    {
        if (mode is not ("original" or "variant" or "mixed") ||
            mode != "original" && !rule.AllowVariants) throw new ArgumentException("Question mode unavailable.");
        var groups = catalog.Where(question => question.PartCode == rule.CatalogPartCode)
            .GroupBy(question => question.BaseCode ?? question.Code).ToArray();
        var candidates = groups.Select(group =>
        {
            var originals = group.Where(question => question.Kind == "original").ToArray();
            var variants = group.Where(question => question.Kind == "variant").ToArray();
            var pool = mode switch
            {
                "original" => originals,
                "variant" => variants,
                _ => variants.Length > 0 && random.Next(2) == 0 ? variants : originals
            };
            return pool.Length == 0 ? null : pool[random.Next(pool.Length)];
        }).Where(question => question != null).Cast<CatalogQuestion>()
            .OrderBy(_ => random.Next()).ToArray();
        if (count is > 0 && candidates.Length < count)
            throw new ArgumentException("Too few questions for this mode.");
        return candidates.Take(count ?? candidates.Length).Select(question =>
        {
            if (!rule.ShuffleAnswers) return question;
            var order = Enumerable.Range(0, question.Answers.Count).OrderBy(_ => random.Next())
                .ToArray();
            return question with
            {
                Answers = order.Select(index => question.Answers[index]).ToArray(),
                CorrectIndex = Array.IndexOf(order, question.CorrectIndex)
            };
        }).ToArray();
    }
}

public static class ExamScoring
{
    public static PartResult Score(SnapshotPart part, IReadOnlyDictionary<string, int> answers,
        bool timedOut)
    {
        if (part.Credited)
            return new PartResult(part.Rule.Code, 0, part.Rule.QuestionCount, true, true, false);
        var correct = part.Questions.Count(question =>
            answers.TryGetValue(question.Code, out var index) && index == question.CorrectIndex);
        return new PartResult(part.Rule.Code, correct, part.Rule.QuestionCount,
            !timedOut && correct >= part.Rule.RequiredCorrect, false, timedOut);
    }

    public static bool Passed(IReadOnlyList<SnapshotPart> parts, IReadOnlyList<PartResult> results) =>
        parts.All(part => results.Any(result => result.Code == part.Rule.Code && result.Passed));
}

public static class ExamEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> Licenses = ["DL-DE-BY-2.0", "CC-BY-4.0", "CC0-1.0"];
    private static readonly HashSet<string> Credits = ["B", "V", "T-N", "T-E", "T-A"];

    public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder app)
    {
        var exams = app.MapGroup("/api/v1/exams").RequireAuthorization(ApiPolicies.ActiveAccount)
            .WithTags("Exams");
        exams.MapGet("/catalogs", Catalogs);
        exams.MapGet("/profiles", Profiles);
        exams.MapGet("/profiles/{id:guid}/forecast", ExamForecastEndpoints.Read);
        exams.MapGet("/credits", ReadCredits);
        exams.MapPut("/credits", AddCredit);
        exams.MapDelete("/credits/{code}", RemoveCredit);
        exams.MapPost("/simulations", Start);
        exams.MapGet("/simulations", History);
        exams.MapGet("/simulations/{id:guid}", ReadSimulation);
        exams.MapPut("/simulations/{id:guid}/answers/{questionCode}", Answer);
        exams.MapPost("/simulations/{id:guid}/parts/{partCode}/finish", FinishPart);
        exams.MapPost("/power-tests", StartPower);
        exams.MapGet("/power-tests", PowerHistory);
        exams.MapGet("/power-tests/{id:guid}", ReadPower);
        exams.MapPut("/power-tests/{id:guid}/answers/{questionCode}", AnswerPower);
        exams.MapPost("/power-tests/{id:guid}/stages/finish", FinishPowerStage);

        var admin = app.MapGroup("/api/v1/exams/admin")
            .RequireAuthorization(ApiPolicies.Admin).WithTags("Exam administration");
        admin.MapGet("/", () => Results.NoContent());
        admin.MapPost("/catalogs/import", Import);
        admin.MapPost("/profiles/versions", PublishProfile);
        return app;
    }

    private static async Task<IResult> Catalogs(LearnPipDbContext db, CancellationToken ct)
    {
        var items = await db.OfficialCatalogEditions.AsNoTracking()
            .OrderBy(item => item.Code).ThenByDescending(item => item.ChangedOn)
            .Select(item => new
            {
                item.Id,
                item.Code,
                item.Title,
                item.Revision,
                item.SourceUrl,
                item.License,
                item.Attribution,
                item.ChangedOn,
                item.ImportedAtUtc
            })
            .ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> Profiles(LearnPipDbContext db, CancellationToken ct)
    {
        var rows = await db.ExamProfileVersions.AsNoTracking()
            .Include(item => item.CatalogEdition)
            .OrderBy(item => item.Code).ThenByDescending(item => item.Version)
            .ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(rows.Select(item => new
        {
            item.Id,
            item.Code,
            item.Title,
            item.AmateurClass,
            item.Version,
            item.CatalogEditionId,
            item.CatalogEdition.Revision,
            Parts = Parse<ProfilePart>(item.PartsJson),
            Sessions = Parse<ExamSession>(item.ScheduleJson),
            item.RulesSourceUrl,
            item.RulesCheckedOn,
            item.CreatedAtUtc
        }).ToArray()));
    }

    private static async Task<IResult> Import(CatalogImportInput input, LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!Valid(input.Code, 80) || !Valid(input.Title, 200) ||
            !Valid(input.Revision, 80) || !Valid(input.Attribution, 500) ||
            !Licenses.Contains(input.License) || !input.RightsConfirmed ||
            !Uri.TryCreate(input.SourceUrl, UriKind.Absolute, out var source) ||
            source.Scheme != Uri.UriSchemeHttps || input.SourceUrl.Length > 1000 ||
            input.ChangedOn > DateOnly.FromDateTime(DateTime.UtcNow) ||
            input.Questions is not { Count: > 0 and <= 10000 } ||
            input.Questions.Select(item => item.Code).Distinct(StringComparer.Ordinal).Count() !=
            input.Questions.Count || input.Questions.Any(item =>
                !Valid(item.Code, 80) || !Valid(item.PartCode, 32) ||
                !Valid(item.Prompt, 12000) || item.Answers is not { Count: >= 2 and <= 6 } ||
                item.Answers.Any(answer => !Valid(answer, 4000)) ||
                item.CorrectIndex < 0 || item.CorrectIndex >= item.Answers.Count ||
                item.Kind is not ("original" or "variant") ||
                item.Kind == "original" && item.BaseCode != null ||
                item.Kind == "variant" && (!Valid(item.BaseCode, 80) ||
                    !input.Questions.Any(original => original.Code == item.BaseCode &&
                        original.Kind == "original" && original.PartCode == item.PartCode))))
            return Invalid("catalog", "Provide a source, compatible reuse license and valid unique questions.");
        if (await db.OfficialCatalogEditions.AnyAsync(item => item.Code == input.Code &&
            item.Revision == input.Revision, ct)) return Results.Conflict();
        var edition = new OfficialCatalogEdition
        {
            Code = input.Code.Trim(),
            Title = input.Title.Trim(),
            Revision = input.Revision.Trim(),
            SourceUrl = input.SourceUrl,
            License = input.License,
            Attribution = input.Attribution.Trim(),
            ChangedOn = input.ChangedOn,
            QuestionsJson = JsonSerializer.Serialize(input.Questions, Json)
        };
        db.OfficialCatalogEditions.Add(edition);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/exams/catalogs", new ApiResponse<object>(new
        {
            edition.Id,
            edition.Code,
            edition.Revision,
            Count = input.Questions.Count
        }));
    }

    private static async Task<IResult> PublishProfile(ProfileInput input, LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!Valid(input.Code, 80) || !Valid(input.Title, 200) ||
            input.AmateurClass is not (string.Empty or "N" or "E" or "A") ||
            input.Parts is not { Count: > 0 and <= 20 } ||
            input.Parts.Select(part => part.Code).Distinct(StringComparer.Ordinal).Count() !=
            input.Parts.Count || input.Parts.Any(part =>
                !Valid(part.Code, 32) || !Valid(part.Title, 120) ||
                !Valid(part.CatalogPartCode, 32) || part.QuestionCount is < 1 or > 100 ||
                part.TimeLimitMinutes is < 1 or > 240 ||
                part.RequiredCorrect < 1 || part.RequiredCorrect > part.QuestionCount ||
                part.CreditCode != null && !Credits.Contains(part.CreditCode) ||
                input.AmateurClass != string.Empty && !part.ShuffleAnswers))
            return Invalid("profile", "Provide unique sections with valid counts, limits and pass rules.");
        if (input.AmateurClass != string.Empty && (input.Parts.Count != 3 ||
            input.Parts.Select(part => part.CreditCode).ToHashSet().SetEquals(
                ["B", "V", "T-" + input.AmateurClass]) == false))
            return Invalid("parts", "Amateur radio profiles require B, V and the matching technical part.");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sessions = input.Sessions ?? [];
        if (sessions.Count > 100 || sessions.Any(session =>
                !Valid(session.Place, 200) || !Https(session.SourceUrl) ||
                session.CheckedOn > today || session.Date < session.CheckedOn ||
                session.RegistrationDeadline > session.Date) ||
            sessions.Select(session => (session.Date, session.Place)).Distinct().Count() !=
                sessions.Count || sessions.Count > 0 &&
            (!Https(input.RulesSourceUrl) || input.RulesCheckedOn is null ||
                input.RulesCheckedOn > today) ||
            (input.RulesSourceUrl != null || input.RulesCheckedOn != null) &&
            (!Https(input.RulesSourceUrl) || input.RulesCheckedOn is null ||
                input.RulesCheckedOn > today))
            return Invalid("sessions", "Verify dates, source URLs, checked dates and rules before publishing.");
        var edition = await db.OfficialCatalogEditions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.CatalogEditionId, ct);
        if (edition == null) return Results.NotFound();
        var questions = Parse<CatalogQuestion>(edition.QuestionsJson);
        if (input.Parts.Any(part => questions.Count(question =>
                question.PartCode == part.CatalogPartCode && question.Kind == "original") <
                part.QuestionCount))
            return Invalid("parts", "The catalog edition has too few questions for a section.");
        var version = (await db.ExamProfileVersions.Where(item => item.Code == input.Code)
            .Select(item => (int?)item.Version).MaxAsync(ct) ?? 0) + 1;
        var profile = new ExamProfileVersion
        {
            Code = input.Code.Trim(),
            Title = input.Title.Trim(),
            AmateurClass = input.AmateurClass,
            Version = version,
            CatalogEditionId = edition.Id,
            PartsJson = JsonSerializer.Serialize(input.Parts, Json),
            ScheduleJson = JsonSerializer.Serialize(sessions, Json),
            RulesSourceUrl = input.RulesSourceUrl,
            RulesCheckedOn = input.RulesCheckedOn
        };
        db.ExamProfileVersions.Add(profile);
        await db.SaveChangesAsync(ct);
        return Results.Created("/api/v1/exams/profiles", new ApiResponse<object>(new
        {
            profile.Id,
            profile.Code,
            profile.Version
        }));
    }

    private static async Task<IResult> ReadCredits(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var items = await db.AccountExamCredits.AsNoTracking()
            .Where(item => item.AccountId == accountId).OrderBy(item => item.Code)
            .Select(item => new { item.Code, item.ReportedAtUtc }).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> AddCredit(CreditInput input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!Credits.Contains(input.Code)) return Invalid("code", "Choose B, V, T-N, T-E or T-A.");
        if (!await db.AccountExamCredits.AnyAsync(item => item.AccountId == accountId &&
                item.Code == input.Code, ct))
        {
            db.AccountExamCredits.Add(new AccountExamCredit { AccountId = accountId, Code = input.Code });
            await db.SaveChangesAsync(ct);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> RemoveCredit(string code, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await db.AccountExamCredits.Where(item => item.AccountId == accountId &&
            item.Code == code).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Start(StartSimulationInput input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var profile = await db.ExamProfileVersions.AsNoTracking().Include(item => item.CatalogEdition)
            .SingleOrDefaultAsync(item => item.Id == input.ProfileVersionId, ct);
        if (profile == null) return Results.NotFound();
        if (!ValidMode(input.QuestionMode, Parse<ProfilePart>(profile.PartsJson)))
            return Invalid("questionMode", "This profile does not support the selected question mode.");
        var credits = (await db.AccountExamCredits.AsNoTracking()
            .Where(item => item.AccountId == accountId).Select(item => item.Code)
            .ToListAsync(ct)).ToHashSet();
        var catalog = Parse<CatalogQuestion>(profile.CatalogEdition.QuestionsJson);
        if (!HasEnough(catalog, Parse<ProfilePart>(profile.PartsJson), input.QuestionMode, false))
            return Invalid("questionMode", "The catalog has too few eligible questions.");
        var parts = Parse<ProfilePart>(profile.PartsJson).Select(rule =>
        {
            var credited = rule.CreditCode != null && credits.Contains(rule.CreditCode);
            return new SnapshotPart(rule, credited, credited ? [] :
                ExamQuestionSelection.Select(catalog, rule, input.QuestionMode,
                    rule.QuestionCount, Random.Shared));
        }).ToArray();
        var now = DateTimeOffset.UtcNow;
        var initialResults = parts.Where(part => part.Credited).Select(part =>
            ExamScoring.Score(part, new Dictionary<string, int>(), false)).ToArray();
        var current = Array.FindIndex(parts, part => !part.Credited);
        var simulation = new ExamSimulation
        {
            AccountId = accountId,
            ProfileVersionId = profile.Id,
            SnapshotJson = JsonSerializer.Serialize(parts, Json),
            ResultJson = JsonSerializer.Serialize(initialResults, Json),
            CurrentPartIndex = current < 0 ? parts.Length : current,
            StartedAtUtc = now,
            PartStartedAtUtc = now,
            CompletedAtUtc = current < 0 ? now : null
        };
        db.ExamSimulations.Add(simulation);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/exams/simulations/{simulation.Id}",
            new ApiResponse<SimulationView>(View(simulation, profile, parts)));
    }

    private static async Task<IResult> History(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var rows = await db.ExamSimulations.AsNoTracking()
            .Where(item => item.AccountId == accountId && item.SnapshotJson.StartsWith("["))
            .OrderByDescending(item => item.StartedAtUtc)
            .Take(50).Select(item => new
            {
                item.Id,
                item.ProfileVersionId,
                item.StartedAtUtc,
                item.CompletedAtUtc
            }).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(rows));
    }

    private static async Task<IResult> ReadSimulation(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var simulation = await db.ExamSimulations.AsNoTracking()
            .Include(item => item.ProfileVersion).ThenInclude(item => item.CatalogEdition)
            .SingleOrDefaultAsync(item => item.Id == id && item.AccountId == accountId, ct);
        if (simulation == null) return Results.NotFound();
        if (!simulation.SnapshotJson.StartsWith('[')) return Results.NotFound();
        return Results.Ok(new ApiResponse<SimulationView>(View(simulation, simulation.ProfileVersion,
            Parse<SnapshotPart>(simulation.SnapshotJson))));
    }

    private static async Task<IResult> Answer(Guid id, string questionCode, AnswerSimulationInput input,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var simulation = await Locked(db, id, accountId, ct);
        if (simulation == null) return Results.NotFound();
        if (!simulation.SnapshotJson.StartsWith('[')) return Results.NotFound();
        var parts = Parse<SnapshotPart>(simulation.SnapshotJson);
        if (simulation.CompletedAtUtc != null || simulation.CurrentPartIndex >= parts.Count)
            return Results.Conflict();
        var part = parts[simulation.CurrentPartIndex];
        var question = part.Questions.SingleOrDefault(item => item.Code == questionCode);
        if (question == null) return Results.NotFound();
        if (DateTimeOffset.UtcNow > simulation.PartStartedAtUtc.AddMinutes(part.Rule.TimeLimitMinutes))
            return Results.Conflict(new { error = "Time limit reached; finish this section." });
        if (input.SelectedIndex < 0 || input.SelectedIndex >= question.Answers.Count)
            return Invalid("selectedIndex", "Choose one of this question's answer indices.");
        var answers = JsonSerializer.Deserialize<Dictionary<string, int>>(simulation.AnswersJson, Json)!;
        answers[questionCode] = input.SelectedIndex;
        simulation.AnswersJson = JsonSerializer.Serialize(answers, Json);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> FinishPart(Guid id, string partCode, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var simulation = await Locked(db, id, accountId, ct);
        if (simulation == null) return Results.NotFound();
        if (!simulation.SnapshotJson.StartsWith('[')) return Results.NotFound();
        var parts = Parse<SnapshotPart>(simulation.SnapshotJson);
        if (simulation.CompletedAtUtc != null || simulation.CurrentPartIndex >= parts.Count)
            return Results.Conflict();
        var part = parts[simulation.CurrentPartIndex];
        if (part.Rule.Code != partCode) return Results.NotFound();
        var answers = JsonSerializer.Deserialize<Dictionary<string, int>>(simulation.AnswersJson, Json)!;
        var now = DateTimeOffset.UtcNow;
        var timedOut = now > simulation.PartStartedAtUtc.AddMinutes(part.Rule.TimeLimitMinutes);
        var results = Parse<PartResult>(simulation.ResultJson!);
        results.Add(ExamScoring.Score(part, answers, timedOut));
        simulation.ResultJson = JsonSerializer.Serialize(results, Json);
        var next = parts.FindIndex(simulation.CurrentPartIndex + 1, item => !item.Credited);
        simulation.CurrentPartIndex = next < 0 ? parts.Count : next;
        simulation.PartStartedAtUtc = now;
        if (next < 0) simulation.CompletedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var profile = await db.ExamProfileVersions.AsNoTracking().Include(item => item.CatalogEdition)
            .SingleAsync(item => item.Id == simulation.ProfileVersionId, ct);
        return Results.Ok(new ApiResponse<SimulationView>(View(simulation, profile, parts)));
    }

    private static bool ValidMode(string mode, IReadOnlyList<ProfilePart> parts) =>
        mode == "original" || mode is "variant" or "mixed" &&
        parts.All(part => part.AllowVariants);

    private static bool HasEnough(IReadOnlyList<CatalogQuestion> catalog,
        IReadOnlyList<ProfilePart> parts, string mode, bool all) => parts.All(part =>
        catalog.Where(question => question.PartCode == part.CatalogPartCode &&
            (mode == "variant" ? question.Kind == "variant" : question.Kind == "original"))
            .Select(question => question.BaseCode ?? question.Code).Distinct().Count() >=
            (all ? 1 : part.QuestionCount));

    private static Dictionary<string, int> Answers(ExamSimulation run) =>
        JsonSerializer.Deserialize<Dictionary<string, int>>(run.AnswersJson, Json)!;

    private static IReadOnlyList<CatalogQuestion> PowerQuestions(PowerSnapshot snapshot) =>
        snapshot.Parts.SelectMany(part => part.Questions).ToArray();

    private static async Task<IResult> StartPower(StartPowerTestInput input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var profile = await db.ExamProfileVersions.AsNoTracking().Include(item => item.CatalogEdition)
            .SingleOrDefaultAsync(item => item.Id == input.ProfileVersionId, ct);
        if (profile == null) return Results.NotFound();
        var rules = Parse<ProfilePart>(profile.PartsJson);
        var catalog = Parse<CatalogQuestion>(profile.CatalogEdition.QuestionsJson);
        if (input.StageSize is < 1 or > 100 || !ValidMode(input.QuestionMode, rules) ||
            !HasEnough(catalog, rules, input.QuestionMode, true))
            return Invalid("powerTest", "Choose an available question mode and stage size (1–100).");
        var snapshot = new PowerSnapshot(input.QuestionMode, input.StageSize, rules.Select(rule =>
            new SnapshotPart(rule, false, ExamQuestionSelection.Select(catalog, rule,
                input.QuestionMode, null, Random.Shared))).ToArray());
        var run = new ExamSimulation
        {
            AccountId = accountId,
            ProfileVersionId = profile.Id,
            SnapshotJson = JsonSerializer.Serialize(snapshot, Json),
            CurrentPartIndex = 0
        };
        db.ExamSimulations.Add(run);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/exams/power-tests/{run.Id}",
            new ApiResponse<PowerView>(PowerViewFor(run, profile, snapshot)));
    }

    private static async Task<IResult> PowerHistory(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var rows = await db.ExamSimulations.AsNoTracking()
            .Where(item => item.AccountId == accountId && item.SnapshotJson.StartsWith("{"))
            .OrderByDescending(item => item.StartedAtUtc).Take(50)
            .Select(item => new
            {
                item.Id,
                item.ProfileVersionId,
                item.StartedAtUtc,
                item.CompletedAtUtc
            }).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(rows));
    }

    private static async Task<IResult> ReadPower(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var run = await db.ExamSimulations.AsNoTracking().Include(item => item.ProfileVersion)
            .ThenInclude(item => item.CatalogEdition)
            .SingleOrDefaultAsync(item => item.Id == id && item.AccountId == accountId, ct);
        if (run == null || !run.SnapshotJson.StartsWith('{')) return Results.NotFound();
        return Results.Ok(new ApiResponse<PowerView>(PowerViewFor(run, run.ProfileVersion,
            JsonSerializer.Deserialize<PowerSnapshot>(run.SnapshotJson, Json)!)));
    }

    private static async Task<IResult> AnswerPower(Guid id, string questionCode,
        AnswerSimulationInput input, LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var run = await Locked(db, id, accountId, ct);
        if (run == null || !run.SnapshotJson.StartsWith('{')) return Results.NotFound();
        if (run.CompletedAtUtc != null) return Results.Conflict();
        var snapshot = JsonSerializer.Deserialize<PowerSnapshot>(run.SnapshotJson, Json)!;
        var question = PowerQuestions(snapshot).Skip(run.CurrentPartIndex * snapshot.StageSize)
            .Take(snapshot.StageSize).SingleOrDefault(item => item.Code == questionCode);
        if (question == null) return Results.NotFound();
        if (input.SelectedIndex < 0 || input.SelectedIndex >= question.Answers.Count)
            return Invalid("selectedIndex", "Choose one of this question's answer indices.");
        var answers = Answers(run);
        answers[questionCode] = input.SelectedIndex;
        run.AnswersJson = JsonSerializer.Serialize(answers, Json);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> FinishPowerStage(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var run = await Locked(db, id, accountId, ct);
        if (run == null || !run.SnapshotJson.StartsWith('{')) return Results.NotFound();
        if (run.CompletedAtUtc != null) return Results.Conflict();
        var snapshot = JsonSerializer.Deserialize<PowerSnapshot>(run.SnapshotJson, Json)!;
        run.CurrentPartIndex++;
        if (run.CurrentPartIndex * snapshot.StageSize >= PowerQuestions(snapshot).Count)
            run.CompletedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var profile = await db.ExamProfileVersions.AsNoTracking().Include(item => item.CatalogEdition)
            .SingleAsync(item => item.Id == run.ProfileVersionId, ct);
        return Results.Ok(new ApiResponse<PowerView>(PowerViewFor(run, profile, snapshot)));
    }

    private static PowerView PowerViewFor(ExamSimulation run, ExamProfileVersion profile,
        PowerSnapshot snapshot)
    {
        var all = PowerQuestions(snapshot);
        var answers = Answers(run);
        var current = run.CompletedAtUtc == null ? all.Skip(run.CurrentPartIndex * snapshot.StageSize)
            .Take(snapshot.StageSize).ToArray() : [];
        PowerMistake[] mistakes = run.CompletedAtUtc == null ? [] : snapshot.Parts.SelectMany(part =>
            part.Questions.Where(question => !answers.TryGetValue(question.Code, out var index) ||
                index != question.CorrectIndex).Select(question => new PowerMistake(question.Code,
                    part.Rule.Code, question.Prompt,
                    answers.TryGetValue(question.Code, out var selected) ?
                        question.Answers[selected] : null,
                    question.Answers[question.CorrectIndex]))).ToArray();
        PowerPartResult[] results = run.CompletedAtUtc == null ? [] : snapshot.Parts.Select(part =>
            new PowerPartResult(part.Rule.Code, part.Questions.Count(question =>
                answers.TryGetValue(question.Code, out var selected) &&
                selected == question.CorrectIndex), part.Questions.Count)).ToArray();
        return new PowerView(run.Id, profile.Id, profile.Code, profile.CatalogEdition.Revision,
            snapshot.QuestionMode, Math.Min(run.CurrentPartIndex + 1,
                (all.Count + snapshot.StageSize - 1) / snapshot.StageSize),
            (all.Count + snapshot.StageSize - 1) / snapshot.StageSize, run.CompletedAtUtc,
            current.Select(question => (object)new
            {
                question.Code,
                question.Prompt,
                question.Answers
            }).ToArray(), current.Where(question => answers.ContainsKey(question.Code))
                .ToDictionary(question => question.Code, question => answers[question.Code]),
            results, mistakes);
    }

    private static Task<ExamSimulation?> Locked(LearnPipDbContext db, Guid id, Guid accountId,
        CancellationToken ct) => db.ExamSimulations.FromSqlInterpolated(
            $"SELECT * FROM \"ExamSimulations\" WHERE \"Id\" = {id} AND \"AccountId\" = {accountId} FOR UPDATE")
            .SingleOrDefaultAsync(ct);

    private static SimulationView View(ExamSimulation simulation, ExamProfileVersion profile,
        IReadOnlyList<SnapshotPart> parts)
    {
        var active = simulation.CompletedAtUtc == null ? parts[simulation.CurrentPartIndex] : null;
        var results = Parse<PartResult>(simulation.ResultJson!);
        var visible = active?.Questions.Select(question => (object)new
        {
            question.Code,
            question.Prompt,
            question.Answers
        }).ToArray() ?? [];
        return new SimulationView(simulation.Id, profile.Id, profile.Version, profile.Code,
            profile.CatalogEdition.Revision, simulation.StartedAtUtc, simulation.CompletedAtUtc,
            active?.Rule.Code, active == null ? null :
                simulation.PartStartedAtUtc.AddMinutes(active.Rule.TimeLimitMinutes),
            visible, results, simulation.CompletedAtUtc == null ? null :
                ExamScoring.Passed(parts, results), active == null ?
                new Dictionary<string, int>() : Answers(simulation).Where(pair =>
                    active.Questions.Any(question => question.Code == pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private static List<T> Parse<T>(string json) => JsonSerializer.Deserialize<List<T>>(json, Json)!;
    private static bool Valid(string? value, int max) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= max;
    private static bool Https(string? value) => value is { Length: <= 1000 } &&
        Uri.TryCreate(value, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps;
    private static IResult Invalid(string key, string error) => Results.ValidationProblem(
        new Dictionary<string, string[]> { [key] = [error] });
}
