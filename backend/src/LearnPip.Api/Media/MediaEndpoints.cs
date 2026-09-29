using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Media;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var media = app.MapGroup("/api/v1/media").WithTags("Private media")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        media.MapPost("/", Upload).DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(PrivateImageProcessor.MaxUploadBytes + 1024 * 1024))
            .Produces<ApiResponse<MediaDetails>>(StatusCodes.Status201Created);
        media.MapGet("/{id:guid}/content", Read);
        media.MapDelete("/{id:guid}", Delete);
        return app;
    }

    private static async Task<IResult> Upload(
        IFormFile file, [FromForm] string altText, [FromForm] Guid? questionVersionId,
        IPrivateMediaStore store, LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var description = altText?.Trim();
        if (string.IsNullOrWhiteSpace(description) || description.Length > 300)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["altText"] = ["A description of 1 to 300 characters is required."]
            });
        if (file.Length is < 1 or > PrivateImageProcessor.MaxUploadBytes)
            return Results.Problem("Image exceeds the 5 MiB limit or is empty.", statusCode: 413);
        if (questionVersionId.HasValue && !await db.QuestionVersions.AsNoTracking().AnyAsync(version =>
                version.Id == questionVersionId && version.Question.OwnerAccountId == accountId &&
                version.Question.DeletedAtUtc == null, cancellationToken))
            return Results.NotFound();

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length != file.Length) return Results.BadRequest();
        var image = PrivateImageProcessor.Sanitize(buffer.ToArray(), file.ContentType);
        if (image == null) return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["file"] = ["A valid JPEG or PNG image of at most 4096 × 4096 pixels is required."]
        });

        var asset = new MediaAsset
        {
            OwnerAccountId = accountId,
            QuestionVersionId = questionVersionId,
            StorageKey = $"postgres/{Guid.NewGuid():N}",
            MediaType = image.Value.MediaType,
            AltText = description,
            ByteLength = image.Value.Bytes.Length
        };
        store.Add(asset, image.Value.Bytes);
        await db.SaveChangesAsync(cancellationToken);
        var details = new MediaDetails(asset.Id, asset.MediaType, asset.ByteLength,
            asset.QuestionVersionId, asset.AltText);
        return Results.Created($"/api/v1/media/{asset.Id}", new ApiResponse<MediaDetails>(details));
    }

    private static async Task<IResult> Read(Guid id, IPrivateMediaStore store, LearnPipDbContext db,
        IAuthorizationService authorization, ClaimsPrincipal user, HttpContext context,
        CancellationToken cancellationToken)
    {
        var asset = await AuthorizedAsset(id, db, authorization, user, cancellationToken);
        if (asset == null) return Results.NotFound();
        var bytes = await store.ReadAsync(id, cancellationToken);
        if (bytes == null) return Results.NotFound();
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return Results.File(bytes, asset.MediaType);
    }

    private static async Task<IResult> Delete(Guid id, IPrivateMediaStore store, LearnPipDbContext db,
        IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var asset = await AuthorizedAsset(id, db, authorization, user, cancellationToken);
        if (asset == null) return Results.NotFound();
        store.Remove(asset);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<MediaAsset?> AuthorizedAsset(Guid id, LearnPipDbContext db,
        IAuthorizationService authorization, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var asset = await db.MediaAssets.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return asset != null && (await authorization.AuthorizeAsync(user, asset, ApiPolicies.MediaRead)).Succeeded
            ? asset : null;
    }
}
