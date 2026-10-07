// <copyright file="QuestionManagementEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>Explizite Inhaltsmoderation ohne Zugriff auf Konten, Codes oder Lernstände.</summary>
public static class QuestionManagementEndpoints
{
    /// <summary>Registriert revisionierte Änderungen, bestätigte Löschung und geschützte Medien.</summary>
    /// <param name="app">Routen-Builder.</param>
    /// <returns>Ergänzte Routen.</returns>
    public static IEndpointRouteBuilder MapQuestionManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var moderation = app.MapGroup("/api/v1/moderation/questions").RequireAuthorization(ApiPolicies.ActiveAccount);
        moderation.MapPost("/{id:guid}/revise", (Guid id, QuestionRevisionInput input, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
            QuestionEndpoints.Publish(id, input.Content, db, user, ct, input.Reason, input.ExpectedVersion))
            .RequireQuestionPermissions("readForeign").RequireRateLimiting("content-write");
        moderation.MapPost("/{versionId:guid}/media/{mediaId:guid}", ReadMedia)
            .RequireQuestionPermissions("readForeign");
        moderation.MapPost("/{id:guid}/withdraw", Withdraw).RequireQuestionPermissions("readForeign", "withdraw");
        moderation.MapPost("/delete", Delete).RequireRateLimiting("content-write");
        app.MapPost("/api/v1/questions/delete", Delete).RequireAuthorization(ApiPolicies.ActiveAccount)
            .RequireRateLimiting("content-write");
        return app;
    }

    private static async Task<IResult> ReadMedia(Guid versionId, Guid mediaId, ModerationInspectInput input, LearnPipDbContext db, IPrivateMediaStore store, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actor))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length is < 10 or > 500)
        {
            return Results.BadRequest();
        }

        var publicVersion = await QuestionAccess.PublicVersions(db).AnyAsync(version => version.Id == versionId, ct);
        if (!publicVersion && !await QuestionPermissions.Allows(db, actor, "readPrivate", ct))
        {
            return Results.Forbid();
        }

        var draft = await db.QuestionDrafts.AsNoTracking().Include(item => item.Question)
            .SingleOrDefaultAsync(item => item.QuestionId == versionId && item.Question.DeletedAtUtc == null && !item.Question.Versions.Any(), ct);
        var draftPayload = draft == null ? null : JsonSerializer.Deserialize<QuestionPublishRequest>(draft.PayloadJson);
        var references = draftPayload == null ? Array.Empty<Guid>() : draftPayload.Prompt.Concat(draftPayload.Explanation)
            .Concat(draftPayload.Answers.SelectMany(answer => answer.Blocks)).Where(block => block.MediaId.HasValue).Select(block => block.MediaId!.Value).ToArray();
        var draftOwner = draft?.Question.OwnerAccountId ?? Guid.Empty;
        var image = await db.MediaAssets.AsNoTracking().SingleOrDefaultAsync(
            media => media.Id == mediaId && media.DeletedAtUtc == null &&
            ((media.OwnerAccountId == draftOwner && references.Contains(media.Id)) ||
            db.QuestionContentBlocks.Any(block => block.MediaAssetId == mediaId &&
                ((block.QuestionVersionId == versionId && block.QuestionVersion!.Question.DeletedAtUtc == null) ||
                 (block.AnswerOption != null && block.AnswerOption.QuestionVersionId == versionId && block.AnswerOption.QuestionVersion.Question.DeletedAtUtc == null)))),
            ct);
        if (image == null)
        {
            return Results.NotFound();
        }

        var bytes = await store.ReadAsync(mediaId, ct);
        if (bytes == null)
        {
            return Results.NotFound();
        }

        if (draft == null)
        {
            db.QuestionModerationEvents.Add(new QuestionModerationEvent
            {
                QuestionVersionId = versionId,
                ModeratorAccountId = actor,
                Action = "inspect.media",
                Note = input.Reason.Trim(),
            });
        }
        else
        {
            db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
            {
                ActorAccountId = actor,
                Action = "moderation.draft.media",
                Target = versionId.ToString(),
                NewValue = input.Reason.Trim(),
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.File(bytes, image.MediaType);
    }

    private static async Task<IResult> Withdraw(Guid id, ModerationInspectInput input, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actor))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length is < 10 or > 500)
        {
            return Results.BadRequest();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var question = await db.Questions.FromSqlInterpolated($"SELECT * FROM \"Questions\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(ct);
        if (question == null || question.DeletedAtUtc != null)
        {
            return Results.NotFound();
        }

        if (!await QuestionPermissions.Allows(db, actor, "readPrivate", ct))
        {
            return Results.Forbid();
        }

        var now = DateTimeOffset.UtcNow;
        await db.QuestionVersions.Where(version => version.QuestionId == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(version => version.Visibility, "private"), ct);
        await db.PublicSubmissions.Where(submission => submission.QuestionVersion.QuestionId == id &&
            (submission.Status == "approved" || submission.Status == "pending" || submission.Status == "minor_hold"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(submission => submission.Status, "withdrawn"), ct);
        await db.GroupQuestionShares.Where(share => share.QuestionId == id && share.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(share => share.RevokedAtUtc, now), ct);
        await db.GroupVersionShares.Where(share => share.QuestionVersion.QuestionId == id).ExecuteDeleteAsync(ct);
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = actor,
            Action = "moderation.question.withdraw",
            Target = id.ToString(),
            NewValue = input.Reason.Trim(),
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Delete(QuestionDeleteInput input, LearnPipDbContext db, ClaimsPrincipal user, HttpContext context, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actor))
        {
            return Results.Unauthorized();
        }

        if (!input.Confirmed || input.QuestionIds is not { Count: >= 1 and <= 100 } ||
            input.QuestionIds.Distinct().Count() != input.QuestionIds.Count ||
            string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length is < 10 or > 500)
        {
            return Results.BadRequest(new { error = "Löschung und Grund (10–500 Zeichen) bestätigen; maximal 100 unterschiedliche Fragen." });
        }

        if (input.QuestionIds.Count > 1 && !await QuestionPermissions.Allows(db, actor, "batch", ct))
        {
            return Results.Forbid();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var questions = new List<Question>();
        foreach (var id in input.QuestionIds.Order())
        {
            var question = await db.Questions.FromSqlInterpolated($"SELECT * FROM \"Questions\" WHERE \"Id\" = {id} FOR UPDATE")
                .SingleOrDefaultAsync(ct);
            if (question == null || question.DeletedAtUtc != null)
            {
                return Results.NotFound();
            }

            var own = question.OwnerAccountId == actor;
            if (!own && !context.Request.Path.StartsWithSegments("/api/v1/moderation/questions"))
            {
                return Results.Forbid();
            }

            if ((!own && !await QuestionPermissions.Allows(db, actor, "readForeign", ct)) ||
                !await QuestionPermissions.Allows(db, actor, own ? "deleteOwn" : "deleteForeign", ct) ||
                (!own && !await QuestionPermissions.Allows(db, actor, "readPrivate", ct)))
            {
                return Results.Forbid();
            }

            questions.Add(question);
        }

        foreach (var question in questions)
        {
            question.DeletedAtUtc = DateTimeOffset.UtcNow;
            db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
            {
                ActorAccountId = actor,
                Action = "questions.soft-delete",
                Target = question.Id.ToString(),
                PreviousValue = "active",
                NewValue = input.Reason.Trim(),
            });
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }
}
