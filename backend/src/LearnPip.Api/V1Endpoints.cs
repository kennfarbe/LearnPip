using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api;

public static class V1Endpoints
{
    public static IEndpointRouteBuilder MapV1Endpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1")
            .WithTags("API v1")
            .RequireAuthorization(ApiPolicies.ActiveAccount);

        api.MapGet("/questions", ListQuestions)
            .WithName("ListOwnQuestions")
            .WithSummary("List private questions owned by the current account")
            .Produces<ApiResponse<PageResponse<QuestionSummary>>>()
            .ProducesValidationProblem();

        api.MapGet("/questions/{id}", GetQuestion)
            .WithName("GetQuestion")
            .WithSummary("Read an owned question or one explicitly shared with a group")
            .Produces<ApiResponse<QuestionDetails>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        api.MapGet("/media/{id}", GetMedia)
            .WithName("GetPrivateMedia")
            .WithSummary("Read metadata for an owned private media asset")
            .Produces<ApiResponse<MediaDetails>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        api.MapGet("/groups/{id}/questions", ListGroupQuestions)
            .WithName("ListGroupQuestions")
            .WithSummary("List active questions explicitly shared with a group member")
            .Produces<ApiResponse<PageResponse<QuestionSummary>>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        app.MapGet("/api/v1/public/questions", ListPublicQuestions)
            .WithTags("Public questions");
        app.MapGet("/api/v1/public/questions/{id:guid}/versions/{number:int}", ReadPublicVersion)
            .WithTags("Public questions");

        return app;
    }

    private static async Task<IResult> ListQuestions(
        LearnPipDbContext dbContext, ClaimsPrincipal user, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var invalid = ValidatePage(page, pageSize);
        if (invalid != null)
        {
            return invalid;
        }

        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var questions = dbContext.Questions.AsNoTracking()
            .Where(question => question.OwnerAccountId == accountId && question.DeletedAtUtc == null);

        return Results.Ok(new ApiResponse<PageResponse<QuestionSummary>>(
            await ReadPage(questions, page, pageSize, cancellationToken)));
    }

    private static async Task<IResult> GetQuestion(
        string id, LearnPipDbContext dbContext, IAuthorizationService authorization,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var questionId))
        {
            return InvalidId();
        }

        var question = await dbContext.Questions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == questionId, cancellationToken);
        if (question == null || !(await authorization.AuthorizeAsync(user, question, ApiPolicies.QuestionRead)).Succeeded)
        {
            return Results.NotFound();
        }

        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var version = await QuestionAccess.ReadableVersions(dbContext, accountId).AsNoTracking()
            .Where(item => item.QuestionId == question.Id)
            .OrderByDescending(item => item.VersionNumber)
            .Select(item => new QuestionVersionDetails(item.Id, item.VersionNumber, item.Prompt))
            .FirstOrDefaultAsync(cancellationToken);

        return Results.Ok(new ApiResponse<QuestionDetails>(new QuestionDetails(question.Id, version)));
    }

    private static async Task<IResult> GetMedia(
        string id, LearnPipDbContext dbContext, IAuthorizationService authorization,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var mediaId))
        {
            return InvalidId();
        }

        var media = await dbContext.MediaAssets.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == mediaId, cancellationToken);
        if (media == null || !(await authorization.AuthorizeAsync(user, media, ApiPolicies.MediaRead)).Succeeded)
        {
            return Results.NotFound();
        }

        return Results.Ok(new ApiResponse<MediaDetails>(
            new MediaDetails(media.Id, media.MediaType, media.ByteLength, media.QuestionVersionId, media.AltText)));
    }

    private static async Task<IResult> ListGroupQuestions(
        string id, LearnPipDbContext dbContext, IAuthorizationService authorization,
        ClaimsPrincipal user, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!Guid.TryParse(id, out var groupId))
        {
            return InvalidId();
        }

        var invalid = ValidatePage(page, pageSize);
        if (invalid != null)
        {
            return invalid;
        }

        var group = await dbContext.StudyGroups.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == groupId, cancellationToken);
        if (group == null || !(await authorization.AuthorizeAsync(user, group, ApiPolicies.GroupRead)).Succeeded)
        {
            return Results.NotFound();
        }

        var visible = QuestionAccess.GroupVersions(dbContext, groupId);
        var questions = dbContext.Questions.AsNoTracking()
            .Where(question => visible.Any(version => version.QuestionId == question.Id));

        return Results.Ok(new ApiResponse<PageResponse<QuestionSummary>>(
            await ReadPage(questions, page, pageSize, cancellationToken, visible)));
    }

    private static async Task<IResult> ListPublicQuestions(LearnPipDbContext db,
        CancellationToken cancellationToken, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var invalid = ValidatePage(page, pageSize);
        if (invalid != null) return invalid;
        var visible = QuestionAccess.PublicVersions(db);
        var questions = db.Questions.AsNoTracking()
            .Where(question => visible.Any(version => version.QuestionId == question.Id));
        return Results.Ok(new ApiResponse<PageResponse<QuestionSummary>>(
            await ReadPage(questions, page, pageSize, cancellationToken, visible)));
    }

    private static async Task<IResult> ReadPublicVersion(Guid id, int number,
        LearnPipDbContext db, CancellationToken cancellationToken)
    {
        var versionId = await QuestionAccess.PublicVersions(db).AsNoTracking()
            .Where(version => version.QuestionId == id && version.VersionNumber == number)
            .Select(version => (Guid?)version.Id).SingleOrDefaultAsync(cancellationToken);
        if (!versionId.HasValue) return Results.NotFound();
        return Results.Ok(new ApiResponse<Questions.PublishedQuestionVersion>(
            (await Questions.QuestionEndpoints.LoadVersion(db, versionId.Value, cancellationToken))!));
    }

    private static async Task<PageResponse<QuestionSummary>> ReadPage(
        IQueryable<Question> questions, int page, int pageSize, CancellationToken cancellationToken,
        IQueryable<QuestionVersion>? visibleVersions = null)
    {
        var total = await questions.CountAsync(cancellationToken);
        var visible = visibleVersions ?? questions.SelectMany(question => question.Versions);
        var items = await questions
            .OrderByDescending(question => question.UpdatedAtUtc)
            .ThenBy(question => question.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(question => new QuestionSummary(
                question.Id,
                visible.Where(version => version.QuestionId == question.Id)
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => (int?)version.VersionNumber).FirstOrDefault(),
                visible.Where(version => version.QuestionId == question.Id)
                    .OrderByDescending(version => version.VersionNumber)
                    .Select(version => version.Prompt).FirstOrDefault()))
            .ToListAsync(cancellationToken);
        return new PageResponse<QuestionSummary>(items, page, pageSize, total);
    }

    private static IResult? ValidatePage(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page is < 1 or > 100000)
        {
            errors["page"] = ["Must be between 1 and 100000."];
        }

        if (pageSize is < 1 or > 100)
        {
            errors["pageSize"] = ["Must be between 1 and 100."];
        }

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    private static IResult InvalidId() => Results.ValidationProblem(
        new Dictionary<string, string[]> { ["id"] = ["Must be a valid UUID."] });
}
