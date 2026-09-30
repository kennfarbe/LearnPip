using System.Security.Claims;
using System.Security.Cryptography;
using LearnPip.Api.Identity;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Family;

public sealed record AgeBandInput(string AgeBand);
public sealed record FamilyInviteInput(string Token);
public sealed record FamilyVerificationInput(string Reference);
public sealed record FamilyGoalInput(string Title, DateTimeOffset? TargetAtUtc);

public static class FamilyEndpoints
{
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder app)
    {
        var family = app.MapGroup("/api/v1/family").WithTags("Family")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        family.MapGet("/me", Me);
        family.MapPut("/age-band", SetAgeBand);
        family.MapPost("/invites", Invite).RequireRateLimiting("auth");
        family.MapPost("/invites/redeem", Redeem).RequireRateLimiting("auth");
        family.MapGet("/links", List);
        family.MapPost("/links/{id:guid}/confirm", Confirm);
        family.MapPost("/links/{id:guid}/revoke", Revoke);
        family.MapGet("/links/{id:guid}/overview", Overview);
        family.MapPost("/links/{id:guid}/goals", AddGoal);
        family.MapDelete("/links/{id:guid}/goals/{goalId:guid}", RemoveGoal);
        family.MapPost("/links/{id:guid}/submissions/{versionId:guid}/approve", ApproveSubmission);
        app.MapPost("/api/v1/admin/family/links/{id:guid}/verify", Verify)
            .WithTags("Family verification").RequireAuthorization(ApiPolicies.Admin);
        return app;
    }

    private static bool Actor(ClaimsPrincipal user, out Guid id) =>
        AccountIdentity.TryGetAccountId(user, out id);

    private static async Task<IResult> Me(ClaimsPrincipal user, LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!Actor(user, out var id)) return Results.Unauthorized();
        var ageBand = await db.Accounts.Where(item => item.Id == id)
            .Select(item => item.AgeBand).SingleAsync(ct);
        return Results.Ok(new ApiResponse<object>(new { ageBand }));
    }

    private static async Task<IResult> SetAgeBand(AgeBandInput input, ClaimsPrincipal user,
        LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var id)) return Results.Unauthorized();
        if (input.AgeBand is not ("minor" or "adult")) return Results.BadRequest();
        // Self-declaration can only happen once. Changing age needs a separate verified procedure.
        var updated = await db.Accounts.Where(item => item.Id == id && item.AgeBand == "unknown")
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.AgeBand, input.AgeBand), ct);
        return updated == 1 ? Results.NoContent() : Results.Conflict();
    }

    private static async Task<IResult> Invite(ClaimsPrincipal user, LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!Actor(user, out var childId)) return Results.Unauthorized();
        if (!await db.Accounts.AnyAsync(item => item.Id == childId && item.AgeBand == "minor", ct))
            return Results.Forbid();
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var link = new FamilyLink
        {
            ChildAccountId = childId,
            InviteHash = SessionAuthentication.Hash(token),
            InviteExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1)
        };
        db.FamilyLinks.Add(link);
        Event(db, link.Id, childId, "invited");
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/family/links/{link.Id}",
            new ApiResponse<object>(new { link.Id, token, link.InviteExpiresAtUtc }));
    }

    private static async Task<IResult> Redeem(FamilyInviteInput input, ClaimsPrincipal user,
        LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var parentId)) return Results.Unauthorized();
        if (input.Token is not { Length: 43 } ||
            !await db.Accounts.AnyAsync(item => item.Id == parentId && item.AgeBand == "adult", ct))
            return Results.BadRequest();
        var hash = SessionAuthentication.Hash(input.Token);
        var link = await db.FamilyLinks.SingleOrDefaultAsync(item => item.InviteHash == hash &&
            item.Status == "invited" && item.InviteExpiresAtUtc > DateTimeOffset.UtcNow &&
            item.ChildAccountId != parentId && item.ParentAccountId == null, ct);
        if (link == null) return Results.NotFound();
        // Conditional update makes one-time tokens safe under concurrent redemption.
        var updated = await db.FamilyLinks.Where(item => item.Id == link.Id &&
            item.Status == "invited" && item.ParentAccountId == null &&
            item.InviteExpiresAtUtc > DateTimeOffset.UtcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ParentAccountId, parentId)
                .SetProperty(item => item.Status, "pending"), ct);
        if (updated == 0) return Results.Conflict();
        Event(db, link.Id, parentId, "redeemed");
        await db.SaveChangesAsync(ct);
        return Results.Accepted(value: new ApiResponse<object>(new { link.Id, status = "pending" }));
    }

    private static async Task<IResult> Verify(Guid id, FamilyVerificationInput input,
        ClaimsPrincipal user, LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var adminId)) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Reference) || input.Reference.Trim().Length > 120)
            return Results.BadRequest();
        var link = await db.FamilyLinks.SingleOrDefaultAsync(item => item.Id == id &&
            item.Status == "pending", ct);
        if (link == null || link.ParentAccountId == adminId || link.ChildAccountId == adminId)
            return Results.NotFound();
        if (!await db.Accounts.AnyAsync(item => item.Id == link.ChildAccountId &&
                item.AgeBand == "minor" && item.DeletedAtUtc == null, ct) ||
            !await db.Accounts.AnyAsync(item => item.Id == link.ParentAccountId &&
                item.AgeBand == "adult" && item.DeletedAtUtc == null, ct)) return Results.Conflict();
        // The administrator records an external proof reference, never the raw evidence.
        var changed = await db.FamilyLinks.Where(item => item.Id == id && item.Status == "pending")
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "verified")
                .SetProperty(item => item.VerifiedByAccountId, adminId)
                .SetProperty(item => item.VerificationReference, input.Reference.Trim())
                .SetProperty(item => item.VerifiedAtUtc, DateTimeOffset.UtcNow), ct);
        if (changed == 0) return Results.Conflict();
        Event(db, id, adminId, "verified");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Confirm(Guid id, ClaimsPrincipal user,
        LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var childId)) return Results.Unauthorized();
        var changed = await db.FamilyLinks.Where(item => item.Id == id &&
            item.ChildAccountId == childId && item.Status == "verified" &&
            item.ParentAccountId != null && item.VerifiedByAccountId != null &&
            item.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "active")
                .SetProperty(item => item.ActivatedAtUtc, DateTimeOffset.UtcNow), ct);
        if (changed == 0) return Results.NotFound();
        Event(db, id, childId, "confirmed");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Revoke(Guid id, ClaimsPrincipal user,
        LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var actorId)) return Results.Unauthorized();
        var changed = await db.FamilyLinks.Where(item => item.Id == id &&
            (item.ChildAccountId == actorId || item.ParentAccountId == actorId) &&
            item.Status != "revoked")
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "revoked")
                .SetProperty(item => item.RevokedAtUtc, DateTimeOffset.UtcNow)
                .SetProperty(item => item.RevokedByAccountId, actorId), ct);
        if (changed == 0) return Results.NotFound();
        var link = await db.FamilyLinks.AsNoTracking().SingleAsync(item => item.Id == id, ct);
        await db.PublicSubmissions.Where(item => item.AccountId == link.ChildAccountId &&
            item.GuardianApprovedByAccountId == link.ParentAccountId &&
            item.Status == "minor_hold")
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.GuardianApprovedByAccountId, (Guid?)null)
                .SetProperty(item => item.GuardianApprovedAtUtc, (DateTimeOffset?)null), ct);
        Event(db, id, actorId, "revoked");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> List(ClaimsPrincipal user, LearnPipDbContext db,
        CancellationToken ct)
    {
        if (!Actor(user, out var actorId)) return Results.Unauthorized();
        var links = await db.FamilyLinks.AsNoTracking()
            .Where(item => item.ChildAccountId == actorId || item.ParentAccountId == actorId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new
            {
                item.Id,
                item.ChildAccountId,
                item.ParentAccountId,
                item.Status,
                item.CreatedAtUtc,
                item.VerifiedAtUtc,
                item.ActivatedAtUtc,
                item.RevokedAtUtc
            }).ToListAsync(ct);
        return Results.Ok(new ApiResponse<object>(links));
    }

    private static async Task<IResult> Overview(Guid id, ClaimsPrincipal user,
        LearnPipDbContext db, HttpContext context, CancellationToken ct)
    {
        if (!Actor(user, out var parentId)) return Results.Unauthorized();
        var link = await db.FamilyLinks.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == id && item.Status == "active" && item.RevokedAtUtc == null &&
            item.VerifiedByAccountId != null && item.ActivatedAtUtc != null &&
            (item.ChildAccountId == parentId || item.ParentAccountId == parentId) &&
            db.Accounts.Any(account => account.Id == item.ChildAccountId &&
                account.AgeBand == "minor" && account.DeletedAtUtc == null), ct);
        if (link == null) return Results.NotFound();
        var (review, candidates) = await ReviewEndpoints.LoadWithCandidates(db, link.ChildAccountId, ct);
        var topics = review.Contents.Select(item =>
        {
            var candidate = candidates.First(candidate => candidate.ContentId == item.Id);
            return new { candidate.Subject, Topic = item.Title, item.Mastered };
        }).GroupBy(item => (item.Subject, item.Topic))
            .Select(group => new TopicProgress(group.Key.Subject, group.Key.Topic, group.Count(),
                group.Count(item => item.Mastered), 0))
            .OrderBy(item => item.Subject).ThenBy(item => item.Topic).ToArray();
        var recent = DateTimeOffset.UtcNow.AddDays(-28);
        var sessions = await db.StudySessions.AsNoTracking().Where(item =>
                item.AccountId == link.ChildAccountId && item.CompletedAtUtc >= recent &&
                item.Attempts.Any())
            .Select(item => item.CompletedAtUtc).ToListAsync(ct);
        var goals = await db.FamilyGoals.AsNoTracking().Where(item => item.FamilyLinkId == id)
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new { item.Id, item.Title, item.TargetAtUtc }).ToListAsync(ct);
        context.Response.Headers.CacheControl = "private, no-store";
        return Results.Ok(new ApiResponse<object>(new
        {
            review.TotalContents,
            review.MasteredContents,
            Topics = topics,
            CompletedSessionsLast28Days = sessions.Count,
            ActiveDaysLast28Days = sessions.Where(item => item.HasValue)
                .Select(item => DateOnly.FromDateTime(item!.Value.UtcDateTime)).Distinct().Count(),
            Goals = goals
        }));
    }

    private static async Task<IResult> AddGoal(Guid id, FamilyGoalInput input,
        ClaimsPrincipal user, LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var actorId)) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 160)
            return Results.BadRequest();
        if (!await ActiveParticipant(db, id, actorId, ct)) return Results.NotFound();
        var goal = new FamilyGoal
        {
            FamilyLinkId = id,
            Title = input.Title.Trim(),
            TargetAtUtc = input.TargetAtUtc
        };
        db.FamilyGoals.Add(goal);
        Event(db, id, actorId, "goal-added");
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/family/links/{id}/goals/{goal.Id}",
            new ApiResponse<object>(new { goal.Id, goal.Title, goal.TargetAtUtc }));
    }

    private static async Task<IResult> RemoveGoal(Guid id, Guid goalId, ClaimsPrincipal user,
        LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var actorId)) return Results.Unauthorized();
        if (!await ActiveParticipant(db, id, actorId, ct)) return Results.NotFound();
        var removed = await db.FamilyGoals.Where(item => item.Id == goalId &&
            item.FamilyLinkId == id).ExecuteDeleteAsync(ct);
        if (removed == 0) return Results.NotFound();
        Event(db, id, actorId, "goal-removed");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ApproveSubmission(Guid id, Guid versionId,
        ClaimsPrincipal user, LearnPipDbContext db, CancellationToken ct)
    {
        if (!Actor(user, out var parentId)) return Results.Unauthorized();
        var link = await ActiveParent(db, id, parentId, ct);
        if (link == null) return Results.NotFound();
        var changed = await db.PublicSubmissions.Where(item =>
                item.QuestionVersionId == versionId && item.AccountId == link.ChildAccountId &&
                item.Status == "minor_hold" && item.AgeDeclaration == "minor" &&
                item.QuestionVersion.Question.OwnerAccountId == link.ChildAccountId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.GuardianApprovedByAccountId,
                    parentId)
                .SetProperty(item => item.GuardianApprovedAtUtc, DateTimeOffset.UtcNow), ct);
        if (changed == 0) return Results.NotFound();
        Event(db, id, parentId, "submission-approved");
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static Task<FamilyLink?> ActiveParent(LearnPipDbContext db, Guid id, Guid parentId,
        CancellationToken ct) => db.FamilyLinks.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == id && item.ParentAccountId == parentId && item.Status == "active" &&
            item.RevokedAtUtc == null && item.VerifiedByAccountId != null &&
            item.ActivatedAtUtc != null &&
            db.Accounts.Any(account => account.Id == item.ChildAccountId &&
                account.AgeBand == "minor" && account.DeletedAtUtc == null &&
                account.DisabledAtUtc == null), ct);

    private static Task<bool> ActiveParticipant(LearnPipDbContext db, Guid id, Guid actorId,
        CancellationToken ct) => db.FamilyLinks.AsNoTracking().AnyAsync(item =>
            item.Id == id && item.Status == "active" && item.RevokedAtUtc == null &&
            (item.ChildAccountId == actorId || item.ParentAccountId == actorId), ct);

    private static void Event(LearnPipDbContext db, Guid linkId, Guid actorId, string action) =>
        db.FamilyLinkEvents.Add(new FamilyLinkEvent
        {
            FamilyLinkId = linkId,
            ActorAccountId = actorId,
            Action = action
        });
}
