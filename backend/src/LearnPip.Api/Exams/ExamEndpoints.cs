using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Exams;

public sealed record CatalogQuestion(string Code, string PartCode, string Prompt,
    IReadOnlyList<string> Answers, int CorrectIndex);
public sealed record CatalogImportInput(string Code, string Title, string Revision,
    string SourceUrl, string License, string Attribution, DateOnly ChangedOn,
    bool RightsConfirmed, IReadOnlyList<CatalogQuestion> Questions);
public sealed record ProfilePart(string Code, string Title, string CatalogPartCode,
    int QuestionCount, int TimeLimitMinutes, int RequiredCorrect, string? CreditCode);
public sealed record ProfileInput(string Code, string Title, string AmateurClass,
    Guid CatalogEditionId, IReadOnlyList<ProfilePart> Parts);
public sealed record StartSimulationInput(Guid ProfileVersionId);
public sealed record AnswerSimulationInput(int SelectedIndex);
public sealed record CreditInput(string Code);
public sealed record SnapshotPart(ProfilePart Rule, bool Credited,
    IReadOnlyList<CatalogQuestion> Questions);
public sealed record PartResult(string Code, int Correct, int Total, bool Passed,
    bool Credited, bool TimedOut);
public sealed record SimulationView(Guid Id, Guid ProfileVersionId, int ProfileVersion,
    string ProfileCode, string CatalogRevision, DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc, string? CurrentPartCode, DateTimeOffset? DeadlineAtUtc,
    IReadOnlyList<object> Questions, IReadOnlyList<PartResult> Parts, bool? Passed);

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
        exams.MapGet("/credits", ReadCredits);
        exams.MapPut("/credits", AddCredit);
        exams.MapDelete("/credits/{code}", RemoveCredit);
        exams.MapPost("/simulations", Start);
        exams.MapGet("/simulations", History);
        exams.MapGet("/simulations/{id:guid}", ReadSimulation);
        exams.MapPut("/simulations/{id:guid}/answers/{questionCode}", Answer);
        exams.MapPost("/simulations/{id:guid}/parts/{partCode}/finish", FinishPart);

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
            .Select(item => new { item.Id, item.Code, item.Title, item.Revision, item.SourceUrl,
                item.License, item.Attribution, item.ChangedOn, item.ImportedAtUtc })
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
            item.Id, item.Code, item.Title, item.AmateurClass, item.Version,
            item.CatalogEditionId, item.CatalogEdition.Revision,
            Parts = Parse<ProfilePart>(item.PartsJson)
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
                item.CorrectIndex < 0 || item.CorrectIndex >= item.Answers.Count))
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
            edition.Id, edition.Code, edition.Revision, Count = input.Questions.Count
        }));
    }

    private static async Task<IResult> PublishProfile(ProfileInput input, LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!Valid(input.Code, 80) || !Valid(input.Title, 200) ||
            input.AmateurClass is not ("" or "N" or "E" or "A") ||
            input.Parts is not { Count: > 0 and <= 20 } ||
            input.Parts.Select(part => part.Code).Distinct(StringComparer.Ordinal).Count() !=
            input.Parts.Count || input.Parts.Any(part =>
                !Valid(part.Code, 32) || !Valid(part.Title, 120) ||
                !Valid(part.CatalogPartCode, 32) || part.QuestionCount is < 1 or > 100 ||
                part.TimeLimitMinutes is < 1 or > 240 ||
                part.RequiredCorrect < 1 || part.RequiredCorrect > part.QuestionCount ||
                part.CreditCode != null && !Credits.Contains(part.CreditCode)))
            return Invalid("profile", "Provide unique sections with valid counts, limits and pass rules.");
        if (input.AmateurClass != "" && (input.Parts.Count != 3 ||
            input.Parts.Select(part => part.CreditCode).ToHashSet().SetEquals(
                ["B", "V", "T-" + input.AmateurClass]) == false))
            return Invalid("parts", "Amateur radio profiles require B, V and the matching technical part.");
        var edition = await db.OfficialCatalogEditions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.CatalogEditionId, ct);
        if (edition == null) return Results.NotFound();
        var questions = Parse<CatalogQuestion>(edition.QuestionsJson);
        if (input.Parts.Any(part => questions.Count(question =>
                question.PartCode == part.CatalogPartCode) < part.QuestionCount))
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
            PartsJson = JsonSerializer.Serialize(input.Parts, Json)
        };
        db.ExamProfileVersions.Add(profile);
        await db.SaveChangesAsync(ct);
        return Results.Created("/api/v1/exams/profiles", new ApiResponse<object>(new
        {
            profile.Id, profile.Code, profile.Version
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
        var credits = (await db.AccountExamCredits.AsNoTracking()
            .Where(item => item.AccountId == accountId).Select(item => item.Code)
            .ToListAsync(ct)).ToHashSet();
        var catalog = Parse<CatalogQuestion>(profile.CatalogEdition.QuestionsJson);
        var parts = Parse<ProfilePart>(profile.PartsJson).Select(rule =>
        {
            var credited = rule.CreditCode != null && credits.Contains(rule.CreditCode);
            return new SnapshotPart(rule, credited, credited ? [] : catalog
                .Where(question => question.PartCode == rule.CatalogPartCode)
                .OrderBy(_ => Random.Shared.Next()).Take(rule.QuestionCount).ToArray());
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
            .Where(item => item.AccountId == accountId).OrderByDescending(item => item.StartedAtUtc)
            .Take(50).Select(item => new { item.Id, item.ProfileVersionId, item.StartedAtUtc,
                item.CompletedAtUtc }).ToListAsync(ct);
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
        var parts = Parse<SnapshotPart>(simulation.SnapshotJson);
        if (simulation.CompletedAtUtc != null || simulation.CurrentPartIndex >= parts.Length)
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
        var parts = Parse<SnapshotPart>(simulation.SnapshotJson);
        if (simulation.CompletedAtUtc != null || simulation.CurrentPartIndex >= parts.Length)
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
        simulation.CurrentPartIndex = next < 0 ? parts.Length : next;
        simulation.PartStartedAtUtc = now;
        if (next < 0) simulation.CompletedAtUtc = now;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var profile = await db.ExamProfileVersions.AsNoTracking().Include(item => item.CatalogEdition)
            .SingleAsync(item => item.Id == simulation.ProfileVersionId, ct);
        return Results.Ok(new ApiResponse<SimulationView>(View(simulation, profile, parts)));
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
            question.Code, question.Prompt, question.Answers
        }).ToArray() ?? [];
        return new SimulationView(simulation.Id, profile.Id, profile.Version, profile.Code,
            profile.CatalogEdition.Revision, simulation.StartedAtUtc, simulation.CompletedAtUtc,
            active?.Rule.Code, active == null ? null :
                simulation.PartStartedAtUtc.AddMinutes(active.Rule.TimeLimitMinutes),
            visible, results, simulation.CompletedAtUtc == null ? null :
                ExamScoring.Passed(parts, results));
    }

    private static List<T> Parse<T>(string json) => JsonSerializer.Deserialize<List<T>>(json, Json)!;
    private static bool Valid(string? value, int max) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= max;
    private static IResult Invalid(string key, string error) => Results.ValidationProblem(
        new Dictionary<string, string[]> { [key] = [error] });
}
