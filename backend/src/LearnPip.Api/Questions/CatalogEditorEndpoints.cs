using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

public sealed record CatalogInput(string Name);
public sealed record CatalogView(Guid Id, string Name, int QuestionCount);
public sealed record DraftSaveRequest(QuestionPublishRequest Content, Guid? CatalogId);
public sealed record DraftView(Guid QuestionId, Guid? CatalogId, int LatestVersion,
    DateTimeOffset UpdatedAtUtc, QuestionPublishRequest Content);
public sealed record CatalogMoveRequest(Guid? CatalogId);

public static class CatalogEditorEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEditorEndpoints(this IEndpointRouteBuilder app)
    {
        var catalogs = app.MapGroup("/api/v1/catalogs").WithTags("Private catalogs")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        catalogs.MapGet("/", ListCatalogs);
        catalogs.MapPost("/", CreateCatalog);
        catalogs.MapPut("/{id:guid}", RenameCatalog);
        catalogs.MapDelete("/{id:guid}", DeleteCatalog);
        catalogs.MapGet("/{id:guid}/questions", ListCatalogQuestions);

        var drafts = app.MapGroup("/api/v1/questions").WithTags("Question editor")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        drafts.MapGet("/drafts", ListDrafts);
        drafts.MapPost("/drafts", CreateDraft)
            .WithMetadata(new RequestSizeLimitAttribute(70 * 1024));
        drafts.MapGet("/{id:guid}/draft", ReadDraft);
        drafts.MapPut("/{id:guid}/draft", SaveDraft)
            .WithMetadata(new RequestSizeLimitAttribute(70 * 1024));
        drafts.MapPost("/{id:guid}/publish", PublishDraft);
        drafts.MapPut("/{id:guid}/catalog", MoveQuestion);
        return app;
    }

    private static async Task<IResult> ListCatalogs(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var items = await db.PrivateCatalogs.AsNoTracking()
            .Where(item => item.OwnerAccountId == accountId)
            .OrderBy(item => item.Name)
            .Select(item => new CatalogView(item.Id, item.Name,
                item.Questions.Count(question => question.DeletedAtUtc == null)))
            .ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<IReadOnlyList<CatalogView>>(items));
    }

    private static async Task<IResult> CreateCatalog(CatalogInput input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120) return Results.BadRequest();
        if (await db.PrivateCatalogs.AnyAsync(item => item.OwnerAccountId == accountId &&
                item.Name == name, cancellationToken)) return Results.Conflict();
        var catalog = new PrivateCatalog { OwnerAccountId = accountId, Name = name };
        db.PrivateCatalogs.Add(catalog);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/catalogs/{catalog.Id}",
            new ApiResponse<CatalogView>(new CatalogView(catalog.Id, catalog.Name, 0)));
    }

    private static async Task<IResult> RenameCatalog(Guid id, CatalogInput input,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var catalog = await db.PrivateCatalogs.SingleOrDefaultAsync(item => item.Id == id &&
            item.OwnerAccountId == accountId, cancellationToken);
        if (catalog == null) return Results.NotFound();
        var name = input.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120) return Results.BadRequest();
        if (await db.PrivateCatalogs.AnyAsync(item => item.OwnerAccountId == accountId &&
                item.Name == name && item.Id != id, cancellationToken)) return Results.Conflict();
        catalog.Name = name;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCatalog(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var catalog = await db.PrivateCatalogs.SingleOrDefaultAsync(item => item.Id == id &&
            item.OwnerAccountId == accountId, cancellationToken);
        if (catalog == null) return Results.NotFound();
        db.PrivateCatalogs.Remove(catalog);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListCatalogQuestions(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (!await db.PrivateCatalogs.AnyAsync(item => item.Id == id &&
                item.OwnerAccountId == accountId, cancellationToken)) return Results.NotFound();
        var items = await db.Questions.AsNoTracking()
            .Where(question => question.OwnerAccountId == accountId &&
                question.PrivateCatalogId == id && question.DeletedAtUtc == null)
            .OrderByDescending(question => question.UpdatedAtUtc)
            .Select(question => new
            {
                question.Id,
                question.UpdatedAtUtc,
                LatestVersion = question.Versions.Max(version => (int?)version.VersionNumber) ?? 0,
                HasDraft = question.Draft != null
            })
            .ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> ListDrafts(LearnPipDbContext db, ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var rows = await db.QuestionDrafts.AsNoTracking()
            .Where(item => item.Question.OwnerAccountId == accountId &&
                item.Question.DeletedAtUtc == null)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Select(item => new
            {
                item.QuestionId,
                item.Question.PrivateCatalogId,
                item.UpdatedAtUtc,
                item.PayloadJson,
                LatestVersion = item.Question.Versions.Max(version => (int?)version.VersionNumber) ?? 0
            })
            .ToListAsync(cancellationToken);
        var views = rows.Select(row => new DraftView(row.QuestionId, row.PrivateCatalogId,
            row.LatestVersion, row.UpdatedAtUtc, JsonSerializer.Deserialize<QuestionPublishRequest>(row.PayloadJson)!))
            .ToArray();
        return Results.Ok(new ApiResponse<IReadOnlyList<DraftView>>(views));
    }

    private static async Task<IResult> CreateDraft(DraftSaveRequest input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var json = DraftJson(input.Content);
        if (json == null) return Results.BadRequest();
        if (!await OwnsCatalog(db, input.CatalogId, accountId, cancellationToken)) return Results.NotFound();
        var question = new Question { OwnerAccountId = accountId, PrivateCatalogId = input.CatalogId };
        var content = new LearningContent
        {
            Id = question.Id,
            OwnerAccountId = accountId,
            Title = string.IsNullOrWhiteSpace(input.Content.Topic) ? "Lerninhalt" : input.Content.Topic.Trim()[..Math.Min(120, input.Content.Topic.Trim().Length)]
        };
        question.LearningContent = content;
        var draft = new QuestionDraft { QuestionId = question.Id, PayloadJson = json };
        db.Questions.Add(question);
        db.QuestionDrafts.Add(draft);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/v1/questions/{question.Id}/draft",
            new ApiResponse<DraftView>(new DraftView(question.Id, question.PrivateCatalogId, 0,
                draft.UpdatedAtUtc, input.Content)));
    }

    private static async Task<IResult> ReadDraft(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var row = await db.QuestionDrafts.AsNoTracking()
            .Where(item => item.QuestionId == id && item.Question.OwnerAccountId == accountId &&
                item.Question.DeletedAtUtc == null)
            .Select(item => new
            {
                item.QuestionId,
                item.Question.PrivateCatalogId,
                item.PayloadJson,
                item.UpdatedAtUtc,
                LatestVersion = item.Question.Versions.Max(version => (int?)version.VersionNumber) ?? 0
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (row == null) return Results.NotFound();
        return Results.Ok(new ApiResponse<DraftView>(new DraftView(row.QuestionId, row.PrivateCatalogId,
            row.LatestVersion, row.UpdatedAtUtc,
            JsonSerializer.Deserialize<QuestionPublishRequest>(row.PayloadJson)!)));
    }

    private static async Task<IResult> SaveDraft(Guid id, DraftSaveRequest input,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var json = DraftJson(input.Content);
        if (json == null) return Results.BadRequest();
        var question = await db.Questions.Include(item => item.Draft).SingleOrDefaultAsync(item =>
            item.Id == id && item.OwnerAccountId == accountId && item.DeletedAtUtc == null,
            cancellationToken);
        if (question == null || !await OwnsCatalog(db, input.CatalogId, accountId, cancellationToken))
            return Results.NotFound();
        question.PrivateCatalogId = input.CatalogId;
        question.UpdatedAtUtc = DateTimeOffset.UtcNow;
        if (question.Draft == null)
            db.QuestionDrafts.Add(new QuestionDraft
            {
                QuestionId = id,
                PayloadJson = json,
                UpdatedAtUtc = question.UpdatedAtUtc
            });
        else
        {
            question.Draft.PayloadJson = json;
            question.Draft.UpdatedAtUtc = question.UpdatedAtUtc;
        }
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> PublishDraft(Guid id, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var json = await db.QuestionDrafts.AsNoTracking()
            .Where(item => item.QuestionId == id && item.Question.OwnerAccountId == accountId &&
                item.Question.DeletedAtUtc == null)
            .Select(item => item.PayloadJson).SingleOrDefaultAsync(cancellationToken);
        if (json == null) return Results.NotFound();
        var request = JsonSerializer.Deserialize<QuestionPublishRequest>(json)!;
        return await QuestionEndpoints.Publish(id, request, db, user, cancellationToken);
    }

    private static async Task<IResult> MoveQuestion(Guid id, CatalogMoveRequest input,
        LearnPipDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var question = await db.Questions.SingleOrDefaultAsync(item => item.Id == id &&
            item.OwnerAccountId == accountId && item.DeletedAtUtc == null, cancellationToken);
        if (question == null || !await OwnsCatalog(db, input.CatalogId, accountId, cancellationToken))
            return Results.NotFound();
        question.PrivateCatalogId = input.CatalogId;
        question.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static Task<bool> OwnsCatalog(LearnPipDbContext db, Guid? id, Guid accountId,
        CancellationToken cancellationToken) => id == null ? Task.FromResult(true) :
        db.PrivateCatalogs.AnyAsync(item => item.Id == id && item.OwnerAccountId == accountId,
            cancellationToken);

    private static string? DraftJson(QuestionPublishRequest? content)
    {
        if (content == null) return null;
        var json = JsonSerializer.Serialize(content);
        return json.Length <= 65536 ? json : null;
    }
}
