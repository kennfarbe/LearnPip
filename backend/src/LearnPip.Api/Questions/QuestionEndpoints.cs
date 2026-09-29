using System.Security.Claims;
using System.Text.RegularExpressions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed record ContentBlockInput(string Kind, string? Text, Guid? MediaId);
public sealed record AnswerInput(bool IsCorrect, IReadOnlyList<ContentBlockInput> Blocks);
public sealed record QuestionPublishRequest(
    string SelectionMode, string Subject, string Topic, string Language, string Source,
    string License, IReadOnlyList<ContentBlockInput> Prompt,
    IReadOnlyList<ContentBlockInput> Explanation, IReadOnlyList<AnswerInput> Answers);
public sealed record ContentBlockOutput(string Kind, string? Text, Guid? MediaId, string? AltText);
public sealed record AnswerOutput(Guid Id, bool IsCorrect, IReadOnlyList<ContentBlockOutput> Blocks);
public sealed record PublishedQuestionVersion(
    Guid Id, int Version, string SelectionMode, string Subject, string Topic, string Language,
    string Source, string License, DateTimeOffset PublishedAtUtc, string Visibility,
    IReadOnlyList<ContentBlockOutput> Prompt, IReadOnlyList<ContentBlockOutput> Explanation,
    IReadOnlyList<AnswerOutput> Answers);
public sealed record GradeRequest(Guid VersionId, IReadOnlyList<Guid> SelectedOptionIds);
public sealed record GradeResult(Guid AttemptId, Guid VersionId, bool IsCorrect,
    IReadOnlyList<Guid> SelectedOptionIds, IReadOnlyList<Guid> CorrectOptionIds);
public sealed record VersionVisibilityInput(string Visibility);

public static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        var questions = app.MapGroup("/api/v1/questions").WithTags("Questions")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        questions.MapPost("/", (QuestionPublishRequest request, LearnPipDbContext db,
            ClaimsPrincipal user, CancellationToken cancellationToken) =>
            Publish(null, request, db, user, cancellationToken));
        questions.MapPost("/{id:guid}/versions", (Guid id, QuestionPublishRequest request,
            LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
            Publish(id, request, db, user, cancellationToken));
        questions.MapGet("/{id:guid}/versions/{number:int}", ReadVersion);
        questions.MapPut("/{id:guid}/versions/{number:int}/visibility", SetVisibility);
        questions.MapPost("/{id:guid}/attempts", Grade);
        return app;
    }

    internal static async Task<IResult> Publish(Guid? id, QuestionPublishRequest request,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var error = Validate(request);
        if (error != null) return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = [error]
        });
        var mediaIds = request.Prompt.Concat(request.Explanation)
            .Concat(request.Answers.SelectMany(answer => answer.Blocks))
            .Where(block => block.MediaId.HasValue).Select(block => block.MediaId!.Value)
            .Distinct().ToArray();
        var ownedMedia = await db.MediaAssets.AsNoTracking()
            .Where(media => mediaIds.Contains(media.Id) && media.OwnerAccountId == accountId &&
                media.DeletedAtUtc == null &&
                (media.QuestionVersionId == null || media.QuestionVersion!.Question.DeletedAtUtc == null))
            .Select(media => media.Id).ToArrayAsync(cancellationToken);
        if (ownedMedia.Length != mediaIds.Length) return Results.ValidationProblem(
            new Dictionary<string, string[]> { ["mediaId"] = ["Image is unavailable or not owned by this account."] });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        Question question;
        int nextVersion;
        if (id.HasValue)
        {
            // Serialize concurrent publishes for one question; previous versions remain immutable.
            question = await db.Questions.FromSqlInterpolated(
                $"SELECT * FROM \"Questions\" WHERE \"Id\" = {id.Value} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken) ?? new Question();
            if (question.Id != id.Value || question.OwnerAccountId != accountId || question.DeletedAtUtc != null)
                return Results.NotFound();
            nextVersion = (await db.QuestionVersions.Where(item => item.QuestionId == id.Value)
                .Select(item => (int?)item.VersionNumber).MaxAsync(cancellationToken) ?? 0) + 1;
        }
        else
        {
            question = new Question { OwnerAccountId = accountId };
            question.LearningContent = new LearningContent
            {
                Id = question.Id,
                OwnerAccountId = accountId,
                Title = request.Topic.Trim()
            };
            db.Questions.Add(question);
            nextVersion = 1;
        }

        var now = DateTimeOffset.UtcNow;
        question.UpdatedAtUtc = now;
        var version = new QuestionVersion
        {
            QuestionId = question.Id,
            CreatedByAccountId = accountId,
            VersionNumber = nextVersion,
            SelectionMode = request.SelectionMode,
            Subject = request.Subject.Trim(),
            Topic = request.Topic.Trim(),
            Language = request.Language.Trim(),
            Source = request.Source.Trim(),
            License = request.License.Trim(),
            PublishedAtUtc = now,
            Prompt = Summary(request.Prompt),
            Explanation = request.Explanation.Count == 0 ? null : Summary(request.Explanation)
        };
        db.QuestionVersions.Add(version);
        AddBlocks(db, version.Id, null, "prompt", request.Prompt);
        AddBlocks(db, version.Id, null, "explanation", request.Explanation);
        foreach (var (answer, index) in request.Answers.Select((answer, index) => (answer, index)))
        {
            var option = new AnswerOption
            {
                QuestionVersionId = version.Id,
                SortOrder = index,
                IsCorrect = answer.IsCorrect,
                Text = Summary(answer.Blocks)
            };
            db.AnswerOptions.Add(option);
            AddBlocks(db, null, option.Id, "answer", answer.Blocks);
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var response = await LoadVersion(db, version.Id, cancellationToken);
        return Results.Created($"/api/v1/questions/{question.Id}/versions/{nextVersion}",
            new ApiResponse<PublishedQuestionVersion>(response!));
    }

    private static async Task<IResult> ReadVersion(Guid id, int number, LearnPipDbContext db,
        IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var question = await db.Questions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (question == null ||
            !(await authorization.AuthorizeAsync(user, question, ApiPolicies.QuestionRead)).Succeeded)
            return Results.NotFound();
        var versionId = await QuestionAccess.ReadableVersions(db, accountId).AsNoTracking()
            .Where(item => item.QuestionId == id && item.VersionNumber == number)
            .Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue) return Results.NotFound();
        return Results.Ok(new ApiResponse<PublishedQuestionVersion>(
            (await LoadVersion(db, versionId.Value, cancellationToken))!));
    }

    private static async Task<IResult> Grade(Guid id, GradeRequest request, LearnPipDbContext db,
        IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var question = await db.Questions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id,
            cancellationToken);
        if (question == null || !(await authorization.AuthorizeAsync(user, question, ApiPolicies.QuestionRead)).Succeeded)
            return Results.NotFound();
        var readableIds = QuestionAccess.ReadableVersions(db, accountId).Select(item => item.Id);
        var version = await db.QuestionVersions.AsNoTracking().Include(item => item.AnswerOptions)
            .SingleOrDefaultAsync(item => item.Id == request.VersionId && item.QuestionId == id &&
                readableIds.Contains(item.Id),
                cancellationToken);
        if (version == null) return Results.NotFound();
        var selected = request.SelectedOptionIds;
        if (selected == null || selected.Count == 0 ||
            selected.Count != selected.Distinct().Count() ||
            version.SelectionMode == "single" && selected.Count != 1 ||
            selected.Any(optionId => version.AnswerOptions.All(option => option.Id != optionId)))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["selectedOptionIds"] = ["Select distinct options from this version; single choice requires exactly one."]
            });
        var correct = version.AnswerOptions.Where(option => option.IsCorrect)
            .Select(option => option.Id).Order().ToArray();
        var isCorrect = selected.Count == correct.Length && selected.All(correct.Contains);
        var session = new StudySession { AccountId = accountId, CompletedAtUtc = DateTimeOffset.UtcNow };
        var attempt = new StudyAttempt
        {
            StudySessionId = session.Id,
            QuestionVersionId = version.Id,
            IsCorrect = isCorrect
        };
        db.StudySessions.Add(session);
        db.StudyAttempts.Add(attempt);
        foreach (var optionId in selected)
            db.StudyAttemptSelections.Add(new StudyAttemptSelection
            {
                StudyAttemptId = attempt.Id,
                AnswerOptionId = optionId
            });
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ApiResponse<GradeResult>(new GradeResult(attempt.Id, version.Id,
            isCorrect, selected.ToArray(), correct)));
    }

    private static async Task<IResult> SetVisibility(Guid id, int number, VersionVisibilityInput input,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (input.Visibility is not ("private" or "public")) return Results.BadRequest();
        var changed = await db.QuestionVersions.Where(version => version.QuestionId == id &&
                version.VersionNumber == number && version.Question.OwnerAccountId == accountId &&
                version.Question.DeletedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(version => version.Visibility,
                input.Visibility), cancellationToken);
        return changed == 1 ? Results.NoContent() : Results.NotFound();
    }

    internal static async Task<PublishedQuestionVersion?> LoadVersion(LearnPipDbContext db,
        Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.QuestionVersions.AsNoTracking()
            .Include(item => item.Blocks).ThenInclude(block => block.MediaAsset)
            .Include(item => item.AnswerOptions).ThenInclude(option => option.Blocks)
            .ThenInclude(block => block.MediaAsset)
            .SingleOrDefaultAsync(item => item.Id == versionId, cancellationToken);
        if (version == null) return null;
        static IReadOnlyList<ContentBlockOutput> Convert(IEnumerable<QuestionContentBlock> blocks) =>
            blocks.OrderBy(block => block.SortOrder)
                .Select(block => new ContentBlockOutput(block.Kind, block.Text, block.MediaAssetId,
                    block.MediaAsset?.AltText)).ToArray();
        return new PublishedQuestionVersion(version.Id, version.VersionNumber, version.SelectionMode,
            version.Subject, version.Topic, version.Language, version.Source, version.License,
            version.PublishedAtUtc, version.Visibility,
            Convert(version.Blocks.Where(block => block.Section == "prompt")),
            Convert(version.Blocks.Where(block => block.Section == "explanation")),
            version.AnswerOptions.OrderBy(option => option.SortOrder)
                .Select(option => new AnswerOutput(option.Id, option.IsCorrect, Convert(option.Blocks)))
                .ToArray());
    }

    private static void AddBlocks(LearnPipDbContext db, Guid? versionId, Guid? optionId,
        string section, IReadOnlyList<ContentBlockInput> blocks)
    {
        for (var index = 0; index < blocks.Count; index++)
            db.QuestionContentBlocks.Add(new QuestionContentBlock
            {
                QuestionVersionId = versionId,
                AnswerOptionId = optionId,
                Section = section,
                SortOrder = index,
                Kind = blocks[index].Kind,
                Text = blocks[index].Kind == "text" ? blocks[index].Text!.Trim() : null,
                MediaAssetId = blocks[index].MediaId
            });
    }

    private static string Summary(IEnumerable<ContentBlockInput> blocks) =>
        string.Join(" ", blocks.Select(block => block.Kind == "text" ? block.Text!.Trim() : "[Bild]"));

    private static string? Validate(QuestionPublishRequest request)
    {
        if (request == null || request.SelectionMode is not ("single" or "multiple") ||
            !ValidText(request.Subject, 120) || !ValidText(request.Topic, 120) ||
            !ValidText(request.Source, 500) || !ValidText(request.License, 120) ||
            string.IsNullOrWhiteSpace(request.Language) || request.Language.Length > 35 ||
            !Regex.IsMatch(request.Language, "^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})*$"))
            return "Selection mode and subject, topic, language, source and license are required.";
        if (!ValidBlocks(request.Prompt, true) || !ValidBlocks(request.Explanation, false) ||
            request.Answers is not { Count: >= 2 and <= 8 } ||
            request.Answers.Any(answer => answer == null || !ValidBlocks(answer.Blocks, true) ||
                Summary(answer.Blocks).Length > 4000) ||
            Summary(request.Prompt).Length > 12000 || Summary(request.Explanation).Length > 12000)
            return "Prompt, explanation and 2 to 8 answers must contain valid ordered text or image blocks.";
        var correct = request.Answers.Count(answer => answer.IsCorrect);
        if (request.SelectionMode == "single" && correct != 1 ||
            request.SelectionMode == "multiple" && correct < 2)
            return "Single choice needs one correct answer; multiple choice needs at least two.";
        return null;
    }

    private static bool ValidText(string? value, int max) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= max;

    private static bool ValidBlocks(IReadOnlyList<ContentBlockInput>? blocks, bool required) =>
        blocks != null && blocks.Count <= 20 && (!required || blocks.Count > 0) &&
        blocks.All(block => block != null &&
            (block.Kind == "text" && ValidText(block.Text, 4000) && block.MediaId == null ||
             block.Kind == "image" && block.MediaId.HasValue && block.Text == null));
}
