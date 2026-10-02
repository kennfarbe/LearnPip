// <copyright file="PhotoDraftEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Ai;

public sealed record PhotoExtractInput(Guid MediaId, string Mode, string DisclosureVersion,
    bool Confirmed, string? ReferenceSolutionHint);
public sealed record PhotoRecognition(string DetectedText, string QuestionText, string Subject,
    string Topic, string? Formula, string? DrawingDescription, IReadOnlyList<string> Answers,
    int? SuggestedCorrectIndex, string ComputedSolution, IReadOnlyList<string> Steps,
    string? ReferenceSolution, IReadOnlyList<string> Uncertainties,
    string? Hint = null, string? NextStep = null);
public sealed record PhotoReview(PhotoRecognition Recognition, string Comparison,
    string ComparisonExplanation, Guid MediaId, string Mode, SolutionCheck Verification);
public sealed record PhotoCheckInput(string? Formula, string? ComputedSolution,
    string? ReferenceSolution, string? ChosenAnswer, IReadOnlyList<string>? Steps,
    string? QuestionText);
public sealed record PhotoDraftReviewInput(Guid MediaId, PhotoRecognition Recognition,
    int CorrectIndex, bool Confirmed);

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
            item.Hint is { Length: > 500 } || item.NextStep is { Length: > 500 } ||
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
            "wurden nicht bewiesen.", mediaId, mode,
            SolutionVerifier.Check(item.Formula, item.ComputedSolution, reference,
                item.SuggestedCorrectIndex is int index ? item.Answers[index] : null,
                item.Steps, item.QuestionText));
    }

    private static bool Valid(string? text, int max) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= max;

    private static string Normalize(string text) =>
        string.Concat(text.Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();
}

public static class PhotoDraftEndpoints
{
    public static IResult Check(PhotoCheckInput input) => Results.Ok(new ApiResponse<SolutionCheck>(
        SolutionVerifier.Check(input.Formula, input.ComputedSolution, input.ReferenceSolution,
            input.ChosenAnswer, input.Steps, input.QuestionText)));

    public static async Task<IResult> Save(PhotoDraftReviewInput input, LearnPipDbContext db,
        ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var accountId)) return Results.Unauthorized();
        var item = input.Recognition;
        if (!input.Confirmed || item == null || input.CorrectIndex < 0 ||
            item.Answers is not { Count: >= 2 and <= 8 } ||
            input.CorrectIndex >= item.Answers.Count ||
            item.Answers.Any(answer => string.IsNullOrWhiteSpace(answer) || answer.Length > 4000) ||
            string.IsNullOrWhiteSpace(item.QuestionText) || item.QuestionText.Length > 4000 ||
            item.Formula is { Length: > 2000 } || item.ComputedSolution is { Length: > 4000 } ||
            item.ReferenceSolution is { Length: > 4000 } ||
            item.DetectedText is { Length: > 8000 } ||
            item.DrawingDescription is { Length: > 2000 } ||
            item.Subject is null or { Length: > 120 } ||
            item.Topic is null or { Length: > 120 } ||
            item.Hint is { Length: > 500 } || item.NextStep is { Length: > 500 } ||
            item.Steps is not { Count: <= 12 } ||
            item.Steps.Any(step => step is null or { Length: > 2000 }) ||
            item.Uncertainties is not { Count: <= 12 } ||
            item.Uncertainties.Any(note => note is null or { Length: > 500 }))
            return Results.BadRequest(new { error = "Review the structured fields and select an answer." });
        if (!await db.MediaAssets.AsNoTracking().AnyAsync(media => media.Id == input.MediaId &&
            media.OwnerAccountId == accountId && media.DeletedAtUtc == null, ct))
            return Results.NotFound();
        var check = SolutionVerifier.Check(item.Formula, item.ComputedSolution,
            item.ReferenceSolution, item.Answers[input.CorrectIndex], item.Steps,
            item.QuestionText);
        if (check.Status == "conflict") return Results.Conflict(new { error = check.Reason });
        if (!SolutionVerifier.SafeHint(item.Hint, item.Answers[input.CorrectIndex]) ||
            !SolutionVerifier.SafeHint(item.NextStep, item.Answers[input.CorrectIndex]))
            return Results.Conflict(new { error = "A hint reveals the correct answer." });
        var text = (string value) => new ContentBlockInput("text", value, null);
        var prompt = string.Join("\n", new[] { item.QuestionText.Trim(),
            string.IsNullOrWhiteSpace(item.Formula) ? "" : "Formel: " + item.Formula.Trim() }
            .Where(value => value.Length != 0));
        prompt = prompt[..Math.Min(4000, prompt.Length)];
        var explanation = new[]
        {
            "Prüfstatus: " + check.Status + ". " + check.Reason,
            string.IsNullOrWhiteSpace(item.Hint) ? "" : "[Hinweis] " + item.Hint.Trim(),
            string.IsNullOrWhiteSpace(item.NextStep) ? "" : "[Nächster Schritt] " + item.NextStep.Trim(),
            item.ComputedSolution?.Trim() ?? "",
            string.Join("\n", item.Steps),
            string.IsNullOrWhiteSpace(item.ReferenceSolution) ? "" :
                "Musterlösung aus der Vorlage: " + item.ReferenceSolution.Trim(),
            string.IsNullOrWhiteSpace(item.DrawingDescription) ? "" :
                "Zeichnung: " + item.DrawingDescription.Trim(),
            string.IsNullOrWhiteSpace(item.DetectedText) ? "" :
                "Erkannter Originaltext: " + item.DetectedText.Trim(),
            "Unsicherheiten: " + string.Join("; ", item.Uncertainties)
        }.Where(value => value.Length != 0);
        var explanationText = string.Join("\n", explanation);
        var content = new QuestionPublishRequest("single", item.Subject.Trim(), item.Topic.Trim(),
            "de", "Privater Fotoentwurf", "",
            [text(prompt), new ContentBlockInput("image", null, input.MediaId)],
            [text(explanationText[..Math.Min(4000, explanationText.Length)])],
            item.Answers.Select((answer, index) => new AnswerInput(index == input.CorrectIndex,
                [text(answer.Trim())])).ToArray());
        return await CatalogEditorEndpoints.CreateDraft(new DraftSaveRequest(content, null), db,
            user, ct);
    }

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
            "hint (a first non-spoiler hint), nextStep (a second non-spoiler step), " +
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
