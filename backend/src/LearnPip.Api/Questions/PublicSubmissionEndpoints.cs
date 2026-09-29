using System.Security.Claims;
using LearnPip.Api.Media;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public static class PublicSubmissionEndpoints
{
    public static IEndpointRouteBuilder MapPublicSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var own = app.MapGroup("/api/v1/questions").WithTags("Public submissions")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        own.MapGet("/{id:guid}/versions/{number:int}/submission-preview", Preview);
        own.MapPost("/{id:guid}/versions/{number:int}/submission", Submit);
        own.MapGet("/{id:guid}/versions/{number:int}/submission", Status);

        var moderation = app.MapGroup("/api/v1/moderation/submissions")
            .WithTags("Public moderation").RequireAuthorization(ApiPolicies.Moderation);
        moderation.MapGet("/", Queue);
        moderation.MapGet("/{versionId:guid}", ReviewPreview);
        moderation.MapGet("/{versionId:guid}/media/{mediaId:guid}", ReviewMedia);
        moderation.MapPost("/{versionId:guid}/decision", Decide);
        return app;
    }

    private static async Task<IResult> Preview(Guid id, int number, PublicSubmissionService service,
        ClaimsPrincipal user, HttpContext context, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var preview = await service.PreviewAsync(id, number, accountId, cancellationToken);
        if (preview == null) return Results.NotFound();
        context.Response.Headers.CacheControl = "private, no-store";
        return Results.Ok(new ApiResponse<PublicPreview>(preview));
    }

    private static async Task<IResult> Submit(Guid id, int number, PublicSubmissionInput input,
        PublicSubmissionService service, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var result = await service.SubmitAsync(id, number, accountId, input, cancellationToken);
        return result switch
        {
            "pending" or "minor_hold" => Results.Accepted(value: new ApiResponse<object>(new { status = result })),
            "missing" => Results.NotFound(),
            "conflict" => Results.Conflict(),
            _ => Results.BadRequest()
        };
    }

    private static async Task<IResult> Status(Guid id, int number, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var item = await db.PublicSubmissions.AsNoTracking().Where(submission =>
                submission.QuestionVersion.QuestionId == id &&
                submission.QuestionVersion.VersionNumber == number &&
                submission.AccountId == accountId)
            .Select(submission => new
            {
                submission.Status,
                submission.SubmittedAtUtc,
                submission.ReviewedAtUtc,
                submission.ReviewNote,
                submission.LicenseChoice,
                submission.AuthorAttribution
            }).SingleOrDefaultAsync(cancellationToken);
        return item == null ? Results.NotFound() : Results.Ok(new ApiResponse<object>(item));
    }

    private static async Task<IResult> Queue(LearnPipDbContext db,
        CancellationToken cancellationToken)
    {
        var items = await db.PublicSubmissions.AsNoTracking()
            .Where(submission => submission.Status == "pending" || submission.Status == "minor_hold")
            .OrderBy(submission => submission.SubmittedAtUtc).Take(100)
            .Select(submission => new
            {
                submission.QuestionVersionId,
                submission.Status,
                submission.SubmittedAtUtc,
                submission.AuthorAttribution,
                submission.LicenseChoice,
                Source = submission.QuestionVersion.Source,
                Prompt = submission.QuestionVersion.Prompt
            }).ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> ReviewPreview(Guid versionId, LearnPipDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await Pending(db, versionId, cancellationToken)) return Results.NotFound();
        var version = await QuestionEndpoints.LoadVersion(db, versionId, cancellationToken);
        return version == null ? Results.NotFound() : Results.Ok(new ApiResponse<PublishedQuestionVersion>(version));
    }

    private static async Task<IResult> ReviewMedia(Guid versionId, Guid mediaId,
        LearnPipDbContext db, IPrivateMediaStore store, HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!await Pending(db, versionId, cancellationToken)) return Results.NotFound();
        var asset = await db.MediaAssets.AsNoTracking().SingleOrDefaultAsync(media =>
            media.Id == mediaId && media.DeletedAtUtc == null, cancellationToken);
        if (asset == null || !await db.QuestionContentBlocks.AnyAsync(block =>
                block.MediaAssetId == mediaId &&
                (block.QuestionVersionId == versionId || block.AnswerOption != null &&
                 block.AnswerOption.QuestionVersionId == versionId) &&
                block.MediaAsset!.OwnerAccountId == asset.OwnerAccountId &&
                db.QuestionVersions.Any(version => version.Id == versionId &&
                    version.Question.OwnerAccountId == asset.OwnerAccountId &&
                    version.Question.DeletedAtUtc == null), cancellationToken)) return Results.NotFound();
        var bytes = await store.ReadAsync(mediaId, cancellationToken);
        if (bytes == null) return Results.NotFound();
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(bytes, asset.MediaType);
    }

    private static async Task<IResult> Decide(Guid versionId, PublicReviewInput input,
        PublicSubmissionService service, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actorId)) return Results.Unauthorized();
        var result = await service.ReviewAsync(versionId, actorId, input, cancellationToken);
        return result switch
        {
            "approved" or "rejected" or "changes_requested" => Results.NoContent(),
            "missing" => Results.NotFound(),
            "minor_hold" or "conflict" => Results.Conflict(new { status = result }),
            _ => Results.BadRequest()
        };
    }

    private static Task<bool> Pending(LearnPipDbContext db, Guid versionId,
        CancellationToken cancellationToken) => db.PublicSubmissions.AsNoTracking().AnyAsync(submission =>
            submission.QuestionVersionId == versionId &&
            (submission.Status == "pending" || submission.Status == "minor_hold") &&
            submission.QuestionVersion.Question.DeletedAtUtc == null, cancellationToken);
}
