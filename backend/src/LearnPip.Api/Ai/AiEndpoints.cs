// <copyright file="AiEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;

/// <summary>
/// Registriert HTTP-Endpunkte für die KI-Einstellungen und die Erstellung von Vorschlägen.
/// </summary>
public static class AiEndpoints
{
    /// <summary>
    /// Registriert HTTP-Endpunkte für die KI-Einstellungen und die Erstellung von Vorschlägen.
    /// </summary>
    /// <param name="app">Der Routen-Builder der API.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var ai = app.MapGroup("/api/v1/ai").WithTags("Optional AI")
            .RequireAuthorization(ApiPolicies.ActiveAccount);
        ai.MapGet("/modes", Modes);
        ai.MapPut("/user-key", StoreKey);
        ai.MapDelete("/user-key", DeleteKey);
        ai.MapPost("/generate", Generate);
        ai.MapPost("/photo/extract", PhotoDraftEndpoints.Extract);
        ai.MapPost(
            "/photo/check",
            PhotoDraftEndpoints.Check)
            .WithMetadata(new RequestSizeLimitAttribute(16 * 1024));
        ai.MapPost(
            "/photo/drafts",
            PhotoDraftEndpoints.Save)
            .WithMetadata(new RequestSizeLimitAttribute(70 * 1024));
        return app;
    }

    /// <summary>
    /// Reserviert eine KI-Anfrage innerhalb der konfigurierten Tagesquote.
    /// </summary>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="mode">Der ausgewählte KI-Betriebsmodus.</param>
    /// <param name="dailyQuota">Das tägliche Nutzungslimit.</param>
    /// <param name="ct">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    internal static async Task<bool> Reserve(
        LearnPipDbContext db,
        Guid accountId,
        string mode,
        int dailyQuota,
        CancellationToken ct)
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"AiDailyUsages\" (\"AccountId\", \"Day\", \"Mode\", \"UsedRequests\") VALUES ({accountId}, {day}, {mode}, 0) ON CONFLICT DO NOTHING",
            ct);
        return await db.AiDailyUsages.Where(item => item.AccountId == accountId &&
            item.Day == day && item.Mode == mode && item.UsedRequests < dailyQuota)
            .ExecuteUpdateAsync(
            setters => setters.SetProperty(
                item => item.UsedRequests,
                item => item.UsedRequests + 1),
            ct) == 1;
    }

    private static async Task<IResult> Modes(
        LearnPipDbContext db,
        IConfiguration config,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        var hasKey = await db.UserAiCredentials.AsNoTracking()
            .AnyAsync(item => item.AccountId == accountId, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var counts = await db.AiDailyUsages.AsNoTracking()
            .Where(item => item.AccountId == accountId && item.Day == today)
            .ToDictionaryAsync(item => item.Mode, item => item.UsedRequests, ct);
        return Results.Ok(new ApiResponse<object>(new
        {
            DisclosureVersion = AiPolicy.DisclosureVersion,
            HasUserKey = hasKey,
            CanConfigureUserKey = AiPolicy.Describe("user-key", config, true).Available,
            Modes = AiPolicy.Modes.Select(mode => new
            {
                Info = AiPolicy.Describe(mode, config, hasKey),
                UsedToday = counts.GetValueOrDefault(mode),
            }).ToArray(),
        }));
    }

    private static async Task<IResult> StoreKey(
        AiKeyInput input,
        LearnPipDbContext db,
        IConfiguration config,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        if (!AiPolicy.Describe("user-key", config, true).Available)
        {
            return Results.Conflict(new { error = "User-key mode is not configured." });
        }

        if (input.Key is null || input.Key.Length is < 16 or > 256 ||
            input.Key.Any(char.IsWhiteSpace))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["key"] = ["Provide an API key of 16–256 characters without whitespace."],
            });
        }

        var ciphertext = AiKeyVault.Seal(input.Key, accountId, config);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"UserAiCredentials\" (\"AccountId\", \"Ciphertext\", \"UpdatedAtUtc\") VALUES ({accountId}, {ciphertext}, {DateTimeOffset.UtcNow}) ON CONFLICT (\"AccountId\") DO UPDATE SET \"Ciphertext\" = EXCLUDED.\"Ciphertext\", \"UpdatedAtUtc\" = EXCLUDED.\"UpdatedAtUtc\"",
            ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteKey(
        LearnPipDbContext db,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        await db.UserAiCredentials.Where(item => item.AccountId == accountId).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Generate(
        AiGenerateInput input,
        LearnPipDbContext db,
        IConfiguration config,
        AiGateway gateway,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId))
        {
            return Results.Unauthorized();
        }

        if (input.Mode == "off")
        {
            return Results.NoContent();
        }

        if (!AiPolicy.Modes.Contains(input.Mode, StringComparer.Ordinal))
        {
            return Results.BadRequest(new { error = "Unknown AI mode." });
        }

        if (!input.Confirmed || input.DisclosureVersion != AiPolicy.DisclosureVersion)
        {
            return Results.BadRequest(new { error = "Confirm the current data transfer notice first." });
        }

        var keyRow = input.Mode == "user-key" ? await db.UserAiCredentials.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId, ct) : null;
        var info = AiPolicy.Describe(input.Mode, config, keyRow != null);
        if (!info.Available)
        {
            return Results.Conflict(new { error = "Selected AI mode unavailable." });
        }

        if (string.IsNullOrWhiteSpace(input.Prompt) ||
            Encoding.UTF8.GetByteCount(input.Prompt) > info.MaxInputBytes)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["prompt"] = ["Text is empty or exceeds the configured byte limit."],
            });
        }

        string? userKey = null;
        if (keyRow != null)
        {
            try
            {
                userKey = AiKeyVault.Open(keyRow.Ciphertext, accountId, config);
            }
            catch (Exception error) when (error is FormatException or CryptographicException)
            {
                return Results.Conflict(new { error = "Stored key unavailable; replace it." });
            }
        }

        if (!await Reserve(db, accountId, input.Mode, info.DailyQuota, ct))
        {
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        }

        try
        {
            var result = await gateway.Resolve(
                input.Mode,
                config,
                userKey)
                .GenerateAsync(input.Prompt, ct);
            return Results.Ok(new ApiResponse<object>(new { Text = result, input.Mode }));
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException or
            System.Text.Json.JsonException or OperationCanceledException or KeyNotFoundException or
            InvalidOperationException)
        {
            return Results.Problem(
                "The selected provider could not process the request.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
