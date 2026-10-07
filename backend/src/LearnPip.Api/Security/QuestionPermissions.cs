// <copyright file="QuestionPermissions.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Security;

/// <summary>Instanzweite Fragenrechte; Rollen werden bei jeder Prüfung aus der Datenbank gelesen.</summary>
public static class QuestionPermissions
{
    /// <summary>Holt bekannte, einzeln konfigurierbare Aktionen.</summary>
    public static IReadOnlyList<string> Actions { get; } = new[]
    {
        "create", "readOwn", "editOwn", "deleteOwn", "readShared", "readForeign",
        "readPrivate", "editForeign", "deleteForeign", "approve", "withdraw", "reports",
        "batch", "import", "export", "community",
    };

    /// <summary>Liefert sichere Vorgabewerte ohne administrative Rechte.</summary>
    /// <param name="role">Benutzer oder Moderator.</param>
    /// <returns>Die Fragenrechte.</returns>
    public static Dictionary<string, bool> Defaults(string role) => Actions.ToDictionary(
        action => action,
        action => role == "moderator" || action is "create" or "readOwn" or "editOwn" or
            "deleteOwn" or "readShared" or "import" or "export" or "community",
        StringComparer.Ordinal);

    /// <summary>Begrenzt Einstellungen auf die aktuelle Systemrolle.</summary>
    /// <param name="db">Datenbank.</param>
    /// <param name="accountId">Aktuelles Konto.</param>
    /// <param name="action">Die bekannte Einzelaktion.</param>
    /// <returns>Rollenbezogene Konfiguration.</returns>
    public static IQueryable<LearnPip.Data.Domain.SystemSetting> Settings(LearnPipDbContext db, Guid accountId, string action) =>
        db.SystemSettings.Where(setting => setting.Key ==
            (db.AccountRoles.Any(grant => grant.AccountId == accountId &&
                grant.RoleDefinition.Scope == "system" && grant.RoleDefinition.Code == "moderator")
                ? "questions.permissions.moderator." + action : "questions.permissions.user." + action));

    /// <summary>Prüft aktuelle aktive Konten und eine einzelne Aktion; fehlende Werte verweigern Zugriff.</summary>
    /// <param name="db">Datenbank.</param>
    /// <param name="accountId">Aktuelles Konto.</param>
    /// <param name="action">Bekannte Aktion.</param>
    /// <param name="ct">Abbruchtoken.</param>
    /// <returns>Ob die Aktion erlaubt ist.</returns>
    public static async Task<bool> Allows(LearnPipDbContext db, Guid accountId, string action, CancellationToken ct)
    {
        return Actions.Contains(action, StringComparer.Ordinal) &&
            await Enabled(db, accountId, action).AnyAsync(ct);
    }

    /// <summary>Verarbeitet ausschließlich vollständige bekannte boolesche Rechte.</summary>
    /// <param name="value">Gespeicherte Konfiguration.</param>
    /// <returns>Rechte oder eine leere, verweigernde Konfiguration.</returns>
    public static Dictionary<string, bool> Parse(string? value)
    {
        try
        {
            var rights = value == null ? null : JsonSerializer.Deserialize<Dictionary<string, bool>>(value);
            return rights != null && rights.Count == Actions.Count && Actions.All(rights.ContainsKey)
                ? rights : new Dictionary<string, bool>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, bool>(StringComparer.Ordinal);
        }
    }

    /// <summary>Erstellt eine übersetzbare Rechteabfrage für Objektlisten und Medien.</summary>
    /// <param name="db">Datenbank.</param>
    /// <param name="accountId">Konto.</param>
    /// <param name="action">Aktion.</param>
    /// <returns>Nur aktuell berechtigte Konten.</returns>
    public static IQueryable<Guid> Enabled(LearnPipDbContext db, Guid accountId, string action)
    {
        if (!Actions.Contains(action, StringComparer.Ordinal))
        {
            return db.Accounts.Where(account => false).Select(account => account.Id);
        }

        var settings = Settings(db, accountId, action);
        return db.Accounts.Where(account => account.Id == accountId && account.DeletedAtUtc == null &&
            account.DisabledAtUtc == null && (db.AccountRoles.Any(grant => grant.AccountId == accountId &&
                grant.RoleDefinition.Scope == "system" && grant.RoleDefinition.Code == "admin") ||
                settings.Any(setting => setting.Value == "true"))).Select(account => account.Id);
    }

    /// <summary>Liest einen vollständigen Snapshot; fehlende oder ungültige Einzelrechte bleiben verweigert.</summary>
    /// <param name="db">Datenbank.</param>
    /// <param name="role">Bekannte Systemrolle.</param>
    /// <param name="ct">Abbruchtoken.</param>
    /// <returns>Vollständige Matrix mit sicheren Verweigerungen.</returns>
    public static async Task<Dictionary<string, bool>> Snapshot(LearnPipDbContext db, string role, CancellationToken ct)
    {
        var prefix = "questions.permissions." + role + ".";
        var values = await db.SystemSettings.AsNoTracking().Where(setting => setting.Key.StartsWith(prefix))
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, ct);
        return Actions.ToDictionary(action => action, action => values.GetValueOrDefault(prefix + action) == "true", StringComparer.Ordinal);
    }

    /// <summary>Filtert eine einzelne Aktion zentral vor Ausführung eines Endpunktes.</summary>
    /// <param name="builder">Endpunkt oder Routengruppe.</param>
    /// <param name="actions">Sämtliche benötigten Aktionen.</param>
    /// <typeparam name="T">Typ des Routen-Builders.</typeparam>
    /// <returns>Der ergänzte Builder.</returns>
    public static T RequireQuestionPermissions<T>(this T builder, params string[] actions)
        where T : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            if (!AccountIdentity.TryGetAccountId(http.User, out var accountId))
            {
                return Results.Unauthorized();
            }

            var db = http.RequestServices.GetRequiredService<LearnPipDbContext>();
            foreach (var action in actions)
            {
                if (!await Allows(db, accountId, action, http.RequestAborted))
                {
                    return Results.Forbid();
                }
            }

            if (HttpMethods.IsGet(http.Request.Method) && http.Request.Path.StartsWithSegments("/api/v1/moderation"))
            {
                db.AdministrationAuditEvents.Add(new LearnPip.Data.Domain.AdministrationAuditEvent
                {
                    ActorAccountId = accountId,
                    Action = "moderation.content.read",
                    Target = http.Request.Path.ToString(),
                    NewValue = "Prüfung eingereichter oder gemeldeter Fragen",
                });
                await db.SaveChangesAsync(http.RequestAborted);
            }

            http.Response.Headers.CacheControl = "private, no-store";
            return await next(context);
        });
    }
}
