// <copyright file="QuestionPermissionEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text.Json;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Administration;

/// <summary>Verwaltet ausschließlich Fragenrechte; Administratorrechte bleiben unveränderlich.</summary>
public static class QuestionPermissionEndpoints
{
    private static readonly string[] Roles = ["user", "moderator"];
    private static readonly string[] Sensitive = ["readForeign", "readPrivate", "editForeign", "deleteForeign", "approve", "withdraw", "reports", "batch"];

    /// <summary>Registriert Rollenmatrix, sichere Vorgaben und eigene effektive Rechte.</summary>
    /// <param name="app">Routen-Builder.</param>
    /// <returns>Ergänzte Routen.</returns>
    public static IEndpointRouteBuilder MapQuestionPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin/question-permissions").RequireAuthorization(ApiPolicies.Admin);
        admin.MapGet("/", async (LearnPipDbContext db, CancellationToken ct) =>
        {
            var roles = new List<object>();
            foreach (var role in Roles)
            {
                var rights = await QuestionPermissions.Snapshot(db, role, ct);
                roles.Add(new { Role = role, Value = JsonSerializer.Serialize(rights), Rights = rights, Defaults = QuestionPermissions.Defaults(role) });
            }

            return Results.Ok(new ApiResponse<object>(new { Roles = roles }));
        });
        admin.MapPut("/{role}", Save);
        app.MapGet("/api/v1/questions/permissions", async (LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            if (!AccountIdentity.TryGetAccountId(user, out var accountId))
            {
                return Results.Unauthorized();
            }

            var rights = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var action in QuestionPermissions.Actions)
            {
                rights[action] = await QuestionPermissions.Allows(db, accountId, action, ct);
            }

            return Results.Ok(new ApiResponse<object>(rights));
        }).RequireAuthorization(ApiPolicies.ActiveAccount);
        return app;
    }

    private static async Task<IResult> Save(string role, QuestionPermissionInput input, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var actor) || role is not ("user" or "moderator"))
        {
            return Results.BadRequest();
        }

        if (input.Rights == null || QuestionPermissions.Parse(JsonSerializer.Serialize(input.Rights)).Count != QuestionPermissions.Actions.Count || input.Reason?.Length > 500)
        {
            return Results.BadRequest(new { error = "Ungültige oder unvollständige Rechtekonfiguration." });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(119120)", ct);
        var key = "questions.permissions." + role + ".";
        var previous = await QuestionPermissions.Snapshot(db, role, ct);
        var previousValue = JsonSerializer.Serialize(previous);
        if (previousValue != input.ExpectedValue)
        {
            return Results.Conflict(new { error = "Die Rechte wurden inzwischen geändert. Bitte neu laden." });
        }

        var expanded = Sensitive.Any(action => input.Rights[action] && !previous.GetValueOrDefault(action));
        if ((expanded && !input.ForeignAccessConfirmed) ||
            (input.Rights["readPrivate"] && !previous.GetValueOrDefault("readPrivate") && !input.PrivacyConfirmed))
        {
            return Results.BadRequest(new { error = "Weitreichende Rechte und Zugriff auf private Fragen separat bestätigen. Betreiber müssen Zweck, Datenschutzhinweise und Rollenvergabe prüfen; keine Konten oder Lernstände werden freigegeben." });
        }

        var value = JsonSerializer.Serialize(input.Rights);
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
        {
            ActorAccountId = actor,
            Action = "questions.permissions.changed",
            Target = role,
            PreviousValue = previousValue,
            NewValue = value,
        });
        if (!string.IsNullOrWhiteSpace(input.Reason))
        {
            db.AdministrationAuditEvents.Add(new AdministrationAuditEvent
            {
                ActorAccountId = actor,
                Action = "questions.permissions.reason",
                Target = role,
                NewValue = input.Reason.Trim(),
            });
        }

        var settings = await db.SystemSettings.Where(setting => setting.Key.StartsWith(key)).ToListAsync(ct);
        foreach (var action in QuestionPermissions.Actions)
        {
            var setting = settings.SingleOrDefault(item => item.Key == key + action);
            if (setting == null)
            {
                setting = new SystemSetting { Key = key + action };
                db.SystemSettings.Add(setting);
            }

            setting.Value = input.Rights[action] ? "true" : "false";
            setting.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }
}
