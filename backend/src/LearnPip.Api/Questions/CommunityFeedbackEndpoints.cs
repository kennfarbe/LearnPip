// <copyright file="CommunityFeedbackEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>
/// Registriert HTTP-Endpunkte für die Kommentare, Bewertungen und Moderation von Fragen.
/// </summary>
public static class CommunityFeedbackEndpoints
{
    /// <summary>
    /// Registriert HTTP-Endpunkte für die Kommentare, Bewertungen und Moderation von Fragen.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapCommunityFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var feedback = app.MapGroup("/api/v1/questions/{id:guid}/versions/{number:int}/feedback")
            .WithTags("Question feedback").RequireAuthorization(ApiPolicies.ActiveAccount);
        feedback.MapGet("/", Read);
        feedback.MapPost("/reports", Report);
        feedback.MapPost("/comments", Comment).RequireRateLimiting("content-write");
        feedback.MapPut("/helpful", Vote);
        feedback.MapDelete("/helpful", RemoveVote);

        var moderation = app.MapGroup("/api/v1/moderation/feedback")
            .WithTags("Question feedback moderation").RequireAuthorization(ApiPolicies.ActiveAccount)
            .RequireQuestionPermissions("readForeign", "readPrivate", "reports");
        moderation.MapGet("/", Inbox);
        moderation.MapGet("/{versionId:guid}", Detail);
        moderation.MapPost("/{versionId:guid}/actions", Act);

        // Sensitive questions stay separate from ordinary learning endpoints.
        var questions = app.MapGroup("/api/v1/moderation/questions")
            .WithTags("Private question moderation").RequireAuthorization(ApiPolicies.ActiveAccount)
            .RequireQuestionPermissions("readForeign");
        questions.MapPost("/browse", BrowseForModeration).RequireRateLimiting("content-write");
        questions.MapPost("/{versionId:guid}/inspect", InspectForModeration)
            .RequireRateLimiting("content-write");
        return app;
    }

    private static bool ValidModerationReason(string? reason) =>
        !string.IsNullOrWhiteSpace(reason) &&
        reason.Trim().Length is >= 10 and <= 500;

    // The browse call is explicit and audited; it discloses only a bounded index.
    private static async Task<IResult> BrowseForModeration(
        ModerationBrowseInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var moderatorId))
        {
            return Results.Unauthorized();
        }

        if (!ValidModerationReason(input.Reason) || input.Page is < 0 or > 1000)
        {
            return Invalid("reason", "Provide a moderation purpose (10-500 characters) and page 0-1000.");
        }

        var privateAccess = await QuestionPermissions.Allows(db, moderatorId, "readPrivate", cancellationToken);
        var rows = await db.Questions.AsNoTracking().Include(question => question.Draft).Include(question => question.Versions)
            .Where(question => question.DeletedAtUtc == null && (privateAccess || question.Versions.Any(version =>
                version.Visibility == "public" && db.PublicSubmissions.Any(submission => submission.QuestionVersionId == version.Id && submission.Status == "approved"))))
            .OrderBy(question => question.Id).Skip(input.Page * 50).Take(50).ToListAsync(cancellationToken);
        var questionIds = rows.Select(question => question.Id).ToArray();
        var approved = privateAccess ? Array.Empty<Guid>() : await db.PublicSubmissions.AsNoTracking()
            .Where(submission => questionIds.Contains(submission.QuestionVersion.QuestionId) && submission.Status == "approved" && submission.QuestionVersion.Visibility == "public")
            .Select(submission => submission.QuestionVersionId).ToArrayAsync(cancellationToken);
        var items = rows.Select(question =>
        {
            var version = question.Versions.Where(version => privateAccess || approved.Contains(version.Id))
                .MaxBy(version => version.VersionNumber);
            var draft = question.Draft == null ? null : JsonSerializer.Deserialize<QuestionPublishRequest>(question.Draft.PayloadJson);
            return new
            {
                QuestionId = question.Id,
                VersionId = version?.Id ?? question.Id,
                VersionNumber = version?.VersionNumber ?? 0,
                Visibility = version?.Visibility ?? "private",
                Subject = version?.Subject ?? draft?.Subject ?? "Entwurf",
                Topic = version?.Topic ?? draft?.Topic ?? "Unvollständige Frage",
            };
        }).ToArray();

        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = moderatorId,
            Action = "moderation.questions.browse",
            Target = $"questions:page:{input.Page}",
            NewValue = input.Reason.Trim(),
        });
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(items));
    }

    // The ordinary question read policy is not broadened by this endpoint.
    private static async Task<IResult> InspectForModeration(
        Guid versionId,
        ModerationInspectInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var moderatorId))
        {
            return Results.Unauthorized();
        }

        if (!ValidModerationReason(input.Reason))
        {
            return Invalid("reason", "Provide a moderation purpose of 10-500 characters.");
        }

        var version = await db.QuestionVersions.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == versionId && item.Question.DeletedAtUtc == null,
            cancellationToken);
        var privateAccess = await QuestionPermissions.Allows(db, moderatorId, "readPrivate", cancellationToken);
        if (version == null)
        {
            if (!privateAccess)
            {
                return Results.Forbid();
            }

            var json = await db.QuestionDrafts.AsNoTracking().Where(draft => draft.QuestionId == versionId &&
                draft.Question.DeletedAtUtc == null && !draft.Question.Versions.Any()).Select(draft => draft.PayloadJson).SingleOrDefaultAsync(cancellationToken);
            if (json == null)
            {
                return Results.NotFound();
            }

            db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
            {
                ActorAccountId = moderatorId,
                Action = "moderation.draft.inspect",
                Target = versionId.ToString(),
                NewValue = input.Reason.Trim(),
            });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new ApiResponse<QuestionPublishRequest>(JsonSerializer.Deserialize<QuestionPublishRequest>(json)!));
        }

        if (!privateAccess && (version.Visibility != "public" || !await db.PublicSubmissions.AnyAsync(
            submission => submission.QuestionVersionId == version.Id && submission.Status == "approved",
            cancellationToken)))
        {
            return Results.Forbid();
        }

        var result = await QuestionEndpoints.LoadVersion(db, versionId, cancellationToken);
        if (result == null)
        {
            return Results.NotFound();
        }

        db.QuestionModerationEvents.Add(new QuestionModerationEvent
        {
            QuestionVersionId = versionId,
            ModeratorAccountId = moderatorId,
            Action = "inspect",
            Note = input.Reason.Trim(),
        });
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ApiResponse<PublishedQuestionVersion>(result));
    }

    private static async Task<Guid?> ReadableVersion(
        Guid id,
        int number,
        Guid accountId,
        LearnPipDbContext db,
        CancellationToken cancellationToken) =>
        await QuestionAccess.ReadableVersions(db, accountId).AsNoTracking()
            .Where(version => version.QuestionId == id && version.VersionNumber == number)
            .Select(version => (Guid?)version.Id).SingleOrDefaultAsync(cancellationToken);

    private static async Task<IResult> Read(
        Guid id,
        int number,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var versionId = await ReadableVersion(id, number, accountId, db, cancellationToken);
        if (versionId == null)
        {
            return Results.NotFound();
        }

        var comments = await db.QuestionComments.AsNoTracking()
            .Where(comment => comment.QuestionVersionId == versionId && comment.RemovedAtUtc == null)
            .OrderBy(comment => comment.CreatedAtUtc).Take(200)
            .Select(comment => new { comment.Id, comment.Text, comment.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        var helpful = await db.QuestionHelpfulVotes.CountAsync(
            vote =>
            vote.QuestionVersionId == versionId && vote.Helpful,
            cancellationToken);
        var unhelpful = await db.QuestionHelpfulVotes.CountAsync(
            vote =>
            vote.QuestionVersionId == versionId && !vote.Helpful,
            cancellationToken);
        var myVote = await db.QuestionHelpfulVotes.AsNoTracking()
            .Where(vote => vote.QuestionVersionId == versionId && vote.AccountId == accountId)
            .Select(vote => (bool?)vote.Helpful).SingleOrDefaultAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(new { comments, helpful, unhelpful, myVote }));
    }

    private static async Task<IResult> Report(
        Guid id,
        int number,
        ReportInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var versionId = await ReadableVersion(id, number, accountId, db, cancellationToken);
        if (versionId == null)
        {
            return Results.NotFound();
        }

        if (input.Reason is not ("incorrect" or "unclear" or "rights" or "privacy" or "other") ||
            string.IsNullOrWhiteSpace(input.Details) || input.Details.Length > 2000)
        {
            return Invalid("report", "Choose a reason and enter up to 2000 characters.");
        }

        if (await db.QuestionReports.AnyAsync(
            report => report.QuestionVersionId == versionId &&
            report.AccountId == accountId && report.Status == "open",
            cancellationToken))
        {
            return Results.Conflict(new { error = "An open report already exists for this version." });
        }

        var report = new QuestionReport
        {
            QuestionVersionId = versionId.Value,
            AccountId = accountId,
            Reason = input.Reason,
            Details = input.Details.Trim(),
        };
        db.QuestionReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/v1/moderation/feedback/{versionId}",
            new ApiResponse<object>(new { report.Id, report.Status }));
    }

    private static async Task<IResult> Comment(
        Guid id,
        int number,
        CommentInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var versionId = await ReadableVersion(id, number, accountId, db, cancellationToken);
        if (versionId == null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > 2000)
        {
            return Invalid("text", "Enter up to 2000 characters.");
        }

        var comment = new QuestionComment
        {
            QuestionVersionId = versionId.Value,
            AccountId = accountId,
            Text = input.Text.Trim(),
        };
        db.QuestionComments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/v1/questions/{id}/versions/{number}/feedback",
            new ApiResponse<object>(new { comment.Id, comment.Text, comment.CreatedAtUtc }));
    }

    private static async Task<IResult> Vote(
        Guid id,
        int number,
        HelpfulInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var versionId = await ReadableVersion(id, number, accountId, db, cancellationToken);
        if (versionId == null)
        {
            return Results.NotFound();
        }

        await UpsertVote(versionId.Value, accountId, input.Helpful, db, cancellationToken);
        return Results.NoContent();
    }

    private static async Task UpsertVote(
        Guid versionId,
        Guid accountId,
        bool helpful,
        LearnPipDbContext db,
        CancellationToken cancellationToken)
    {
        // PostgreSQL's conflict clause prevents duplicate votes during concurrent requests.
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "QuestionHelpfulVotes" ("QuestionVersionId", "AccountId", "Helpful", "UpdatedAtUtc")
            VALUES ({versionId}, {accountId}, {helpful}, {DateTimeOffset.UtcNow})
            ON CONFLICT ("QuestionVersionId", "AccountId") DO UPDATE SET
                "Helpful" = EXCLUDED."Helpful", "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc"
            """,
            cancellationToken);
    }

    private static async Task<IResult> RemoveVote(
        Guid id,
        int number,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var versionId = await ReadableVersion(id, number, accountId, db, cancellationToken);
        if (versionId == null)
        {
            return Results.NotFound();
        }

        await db.QuestionHelpfulVotes.Where(vote => vote.QuestionVersionId == versionId &&
            vote.AccountId == accountId).ExecuteDeleteAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> Inbox(
        LearnPipDbContext db,
        CancellationToken cancellationToken)
    {
        var items = await db.QuestionReports.AsNoTracking().Where(report => report.Status == "open")
            .GroupBy(report => new
            {
                report.QuestionVersionId,
                report.QuestionVersion.QuestionId,
                report.QuestionVersion.VersionNumber,
                report.QuestionVersion.Prompt,
            })
            .Select(group => new
            {
                group.Key.QuestionVersionId,
                group.Key.QuestionId,
                group.Key.VersionNumber,
                group.Key.Prompt,
                OpenReports = group.Count(),
                Oldest = group.Min(report => report.CreatedAtUtc),
            })
            .OrderBy(group => group.Oldest).Take(100).ToListAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(items));
    }

    private static async Task<IResult> Detail(
        Guid versionId,
        LearnPipDbContext db,
        CancellationToken cancellationToken)
    {
        if (!await db.QuestionVersions.AnyAsync(version => version.Id == versionId, cancellationToken))
        {
            return Results.NotFound();
        }

        var reports = await db.QuestionReports.AsNoTracking()
            .Where(report => report.QuestionVersionId == versionId)
            .OrderBy(report => report.CreatedAtUtc)
            .Select(report => new
            {
                report.Id,
                report.Reason,
                report.Details,
                report.Status,
                report.CreatedAtUtc,
                report.ClosedAtUtc,
            }).ToListAsync(cancellationToken);
        var comments = await db.QuestionComments.AsNoTracking()
            .Where(comment => comment.QuestionVersionId == versionId)
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(comment => new { comment.Id, comment.Text, comment.RemovedAtUtc })
            .ToListAsync(cancellationToken);
        var events = await db.QuestionModerationEvents.AsNoTracking()
            .Where(item => item.QuestionVersionId == versionId)
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new
            {
                item.Action,
                item.Note,
                item.CreatedAtUtc,
                item.ReplacementVersionId,
            }).ToListAsync(cancellationToken);
        var helpful = await db.QuestionHelpfulVotes.CountAsync(
            vote => vote.QuestionVersionId == versionId &&
            vote.Helpful,
            cancellationToken);
        var unhelpful = await db.QuestionHelpfulVotes.CountAsync(
            vote => vote.QuestionVersionId == versionId &&
            !vote.Helpful,
            cancellationToken);
        return Results.Ok(new ApiResponse<object>(new
        {
            Version = await QuestionEndpoints.LoadVersion(db, versionId, cancellationToken),
            reports,
            comments,
            events,
            helpful,
            unhelpful,
        }));
    }

    private static async Task<IResult> Act(
        Guid versionId,
        ModerationActionInput input,
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var moderatorId))
        {
            return Results.Unauthorized();
        }

        if (input.Action is not ("close" or "correct" or "withdraw" or "delete" or "remove_comment") ||
            string.IsNullOrWhiteSpace(input.Note) || input.Note.Length > 1000)
        {
            return Invalid("action", "Choose an action and provide a note of up to 1000 characters.");
        }

        if (input.Action == "delete" && !input.Confirmed)
        {
            return Results.BadRequest(new { error = "Löschung ausdrücklich bestätigen." });
        }

        if (input.Action == "correct" && (string.IsNullOrWhiteSpace(input.CorrectedPrompt) ||
            input.CorrectedPrompt.Length > 12000))
        {
            return Invalid("correctedPrompt", "Provide a corrected question of up to 12000 characters.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var version = await db.QuestionVersions.FromSqlInterpolated(
            $"SELECT * FROM \"QuestionVersions\" WHERE \"Id\" = {versionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
        if (version == null)
        {
            return Results.NotFound();
        }

        await db.Entry(version).Reference(item => item.Question).LoadAsync(cancellationToken);
        var own = version.Question.OwnerAccountId == moderatorId;
        var permission = input.Action switch
        {
            "correct" => own ? "editOwn" : "editForeign",
            "delete" => own ? "deleteOwn" : "deleteForeign",
            "withdraw" => "withdraw",
            _ => "reports",
        };
        if (!await QuestionPermissions.Allows(db, moderatorId, permission, cancellationToken))
        {
            return Results.Forbid();
        }

        await db.Entry(version).Collection(item => item.Blocks).LoadAsync(cancellationToken);
        await db.Entry(version).Collection(item => item.AnswerOptions).LoadAsync(cancellationToken);
        foreach (var option in version.AnswerOptions)
        {
            await db.Entry(option).Collection(item => item.Blocks).LoadAsync(cancellationToken);
        }

        if (input.Action != "close" && !await db.QuestionReports.AnyAsync(
            report =>
            report.QuestionVersionId == versionId && report.Status == "open",
            cancellationToken))
        {
            return Results.Conflict(new { error = "No open report exists for this version." });
        }

        var now = DateTimeOffset.UtcNow;
        Guid? replacementId = null;
        if (input.Action == "correct")
        {
            if (version.Question.DeletedAtUtc != null)
            {
                return Results.Conflict();
            }

            await db.Questions.FromSqlInterpolated(
                $"SELECT * FROM \"Questions\" WHERE \"Id\" = {version.QuestionId} FOR UPDATE")
                .LoadAsync(cancellationToken);
            var next = (await db.QuestionVersions.Where(item => item.QuestionId == version.QuestionId)
                .Select(item => (int?)item.VersionNumber).MaxAsync(cancellationToken) ?? 0) + 1;
            var corrected = new QuestionVersion
            {
                QuestionId = version.QuestionId,
                CreatedByAccountId = moderatorId,
                VersionNumber = next,
                Visibility = "private",
                Prompt = input.CorrectedPrompt!.Trim(),
                Explanation = version.Explanation,
                SelectionMode = version.SelectionMode,
                Subject = version.Subject,
                Topic = version.Topic,
                Language = version.Language,
                Source = version.Source,
                License = version.License,
                AuthorAttribution = version.AuthorAttribution,
                PublishedAtUtc = now,
            };
            db.QuestionVersions.Add(corrected);
            db.QuestionContentBlocks.Add(new QuestionContentBlock
            {
                QuestionVersionId = corrected.Id,
                Section = "prompt",
                SortOrder = 0,
                Kind = "text",
                Text = input.CorrectedPrompt.Trim(),
            });
            foreach (var block in version.Blocks.Where(item => item.Section == "explanation"))
            {
                db.QuestionContentBlocks.Add(new QuestionContentBlock
                {
                    QuestionVersionId = corrected.Id,
                    Section = block.Section,
                    SortOrder = block.SortOrder,
                    Kind = block.Kind,
                    Text = block.Text,
                    MediaAssetId = block.MediaAssetId,
                });
            }

            foreach (var answer in version.AnswerOptions)
            {
                var copy = new AnswerOption
                {
                    QuestionVersionId = corrected.Id,
                    SortOrder = answer.SortOrder,
                    IsCorrect = answer.IsCorrect,
                    Text = answer.Text,
                };
                db.AnswerOptions.Add(copy);
                foreach (var block in answer.Blocks)
                {
                    db.QuestionContentBlocks.Add(new QuestionContentBlock
                    {
                        AnswerOptionId = copy.Id,
                        Section = block.Section,
                        SortOrder = block.SortOrder,
                        Kind = block.Kind,
                        Text = block.Text,
                        MediaAssetId = block.MediaAssetId,
                    });
                }
            }

            replacementId = corrected.Id;
            version.Question.UpdatedAtUtc = now;
        }

        if (input.Action is "correct" or "withdraw" or "delete")
        {
            version.Visibility = "private";
            var submission = await db.PublicSubmissions.SingleOrDefaultAsync(
                item =>
                item.QuestionVersionId == versionId,
                cancellationToken);
            if (submission != null && submission.Status is "approved" or "pending" or "minor_hold")
            {
                submission.Status = "withdrawn";
            }
        }

        if (input.Action == "delete")
        {
            version.Question.DeletedAtUtc = now;
        }

        if (input.Action == "remove_comment")
        {
            // The note carries the comment UUID; details are retained in the audit event.
            if (!Guid.TryParse(input.CorrectedPrompt, out var commentId))
            {
                return Invalid("correctedPrompt", "Provide the comment ID to remove.");
            }

            var comment = await db.QuestionComments.SingleOrDefaultAsync(
                item =>
                item.Id == commentId && item.QuestionVersionId == versionId && item.RemovedAtUtc == null,
                cancellationToken);
            if (comment == null)
            {
                return Results.NotFound();
            }

            comment.RemovedAtUtc = now;
            comment.Text = "[removed by moderation]";
        }

        foreach (var report in await db.QuestionReports.Where(item =>
            item.QuestionVersionId == versionId && item.Status == "open").ToListAsync(cancellationToken))
        {
            report.Status = "closed";
            report.ClosedAtUtc = now;
        }

        db.QuestionModerationEvents.Add(new QuestionModerationEvent
        {
            QuestionVersionId = versionId,
            ModeratorAccountId = moderatorId,
            Action = input.Action,
            Note = input.Note.Trim(),
            ReplacementVersionId = replacementId,
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Results.Ok(new ApiResponse<object>(new { replacementVersionId = replacementId }));
    }

    private static IResult Invalid(string field, string error) => Results.ValidationProblem(
        new Dictionary<string, string[]> { [field] = [error] });
}
