using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;

public sealed record PhotoExtractInput(Guid MediaId, string Mode, string DisclosureVersion,
    bool Confirmed, string? ReferenceSolutionHint);
public sealed record PhotoRecognition(string DetectedText, string QuestionText, string Subject,
    string Topic, string? Formula, string? DrawingDescription, IReadOnlyList<string> Answers,
    int? SuggestedCorrectIndex, string ComputedSolution, IReadOnlyList<string> Steps,
    string? ReferenceSolution, IReadOnlyList<string> Uncertainties);
public sealed record PhotoReview(PhotoRecognition Recognition, string Comparison,
    string ComparisonExplanation, Guid MediaId, string Mode);

public static class PhotoDraftParser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static PhotoReview? Parse(string raw, Guid mediaId, string mode,
        string? referenceHint)
    {
        PhotoRecognition? item;
        try { item = JsonSerializer.Deserialize<PhotoRecognition>(raw, Json); }
        catch (JsonException) { return null; }
        if (item == null || !Valid(item.DetectedText, 8000) ||
            !Valid(item.QuestionText, 4000) || !Valid(item.ComputedSolution, 4000) ||
            item.Subject is null or { Length: > 120 } ||
            item.Topic is null or { Length: > 120 } ||
            item.Formula is { Length: > 2000 } ||
            item.DrawingDescription is { Length: > 2000 } ||
            item.ReferenceSolution is { Length: > 4000 } ||
            item.Answers is not { Count: <= 8 } ||
            item.Answers.Any(answer => !Valid(answer, 4000)) ||
            item.Steps is not { Count: <= 12 } ||
            item.Steps.Any(step => !Valid(step, 2000)) ||
            item.Uncertainties is not { Count: <= 12 } ||
            item.Uncertainties.Any(note => !Valid(note, 500)) ||
            item.SuggestedCorrectIndex is < 0 ||
            item.SuggestedCorrectIndex >= item.Answers.Count) return null;
        var reference = string.IsNullOrWhiteSpace(referenceHint) ? item.ReferenceSolution :
            referenceHint.Trim();
        var comparison = string.IsNullOrWhiteSpace(reference) ? "unknown" :
            Normalize(reference) == Normalize(item.ComputedSolution) ? "same-text" :
            "different-text";
        return new PhotoReview(item with
        {
            ReferenceSolution = reference,
            Uncertainties = item.Uncertainties.Append(
                "KI-Erkennung und Lösung sind ungeprüft; Formeln und Zeichnungen kontrollieren.")
                .Distinct().ToArray()
        }, comparison, comparison == "unknown" ? "Keine Musterlösung aus der Vorlage verfügbar." :
            "Nur Textvergleich mit der Vorlage; mathematische Gleichwertigkeit und Richtigkeit " +
            "wurden nicht bewiesen.", mediaId, mode);
    }

    private static bool Valid(string? text, int max) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= max;

    private static string Normalize(string text) =>
        string.Concat(text.Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();
}

public static class PhotoDraftEndpoints
{
    public static async Task<IResult> Extract(PhotoExtractInput input, LearnPipDbContext db,
        IPrivateMediaStore store, IConfiguration config, AiGateway gateway,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        if (input.Mode == "off" || !AiPolicy.Modes.Contains(input.Mode, StringComparer.Ordinal))
            return Results.Conflict(new { error = "Choose an available image-capable provider." });
        if (!input.Confirmed || input.DisclosureVersion != AiPolicy.DisclosureVersion)
            return Results.BadRequest(new { error = "Confirm the photo transfer notice first." });
        var keyRow = input.Mode == "user-key" ? await db.UserAiCredentials.AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId, ct) : null;
        var info = AiPolicy.Describe(input.Mode, config, keyRow != null);
        if (!info.Available) return Results.Conflict(new { error = "Selected AI mode unavailable." });
        if (input.ReferenceSolutionHint is { Length: > 4000 } ||
            Encoding.UTF8.GetByteCount(input.ReferenceSolutionHint ?? "") > info.MaxInputBytes)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["referenceSolutionHint"] = ["Reference text exceeds the configured limit."]
            });
        var media = await db.MediaAssets.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == input.MediaId && item.OwnerAccountId == accountId &&
            item.DeletedAtUtc == null, ct);
        if (media == null) return Results.NotFound();
        if (media.ByteLength > info.MaxImageBytes)
            return Results.Problem("Image exceeds the AI image limit.", statusCode: 413);
        var image = await store.ReadAsync(media.Id, ct);
        if (image == null || image.Length > info.MaxImageBytes) return Results.NotFound();
        string? key = null;
        if (keyRow != null)
        {
            try { key = AiKeyVault.Open(keyRow.Ciphertext, accountId, config); }
            catch (Exception error) when (error is FormatException or CryptographicException)
            {
                return Results.Conflict(new { error = "Stored key unavailable; replace it." });
            }
        }
        if (!await AiEndpoints.Reserve(db, accountId, input.Mode, info.DailyQuota, ct))
            return Results.StatusCode(StatusCodes.Status429TooManyRequests);
        const string instruction = "Analyze this educational task image. Return ONLY a JSON object " +
            "with camelCase fields: detectedText, questionText, subject, topic, formula, " +
            "drawingDescription, answers (array of possible answer texts, empty if unknown), " +
            "suggestedCorrectIndex (zero-based or null), computedSolution, steps (array), " +
            "referenceSolution (only if visibly printed in the image, otherwise null), " +
            "uncertainties (array naming unreadable symbols, drawing ambiguities and guesses). " +
            "Transcribe formulas verbatim; do not invent a missing reference answer. " +
            "Explain solution steps and mark uncertainty, never claim verification.";
        var prompt = instruction + (string.IsNullOrWhiteSpace(input.ReferenceSolutionHint) ? "" :
            " User-provided reference solution to compare: " + input.ReferenceSolutionHint);
        try
        {
            var raw = await gateway.Resolve(input.Mode, config, key)
                .AnalyzeImageAsync(prompt, image, media.MediaType, ct);
            var review = PhotoDraftParser.Parse(raw, media.Id, input.Mode,
                input.ReferenceSolutionHint);
            return review == null ? Results.UnprocessableEntity(new
            {
                error = "Recognition did not yield valid structured fields; edit manually."
            }) : Results.Ok(new ApiResponse<PhotoReview>(review));
        }
        catch (Exception error) when (error is HttpRequestException or InvalidDataException or
            JsonException or OperationCanceledException or KeyNotFoundException or
            InvalidOperationException)
        {
            return Results.Problem("The selected provider could not analyze the image.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
