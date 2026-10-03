// <copyright file="PhotoDraftParser.cs" company="LearnPip contributors">
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

/// <summary>
/// Liest strukturierte KI-Bildanalyseergebnisse in einen überprüfbaren Fragenentwurf ein.
/// </summary>
public static class PhotoDraftParser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Liest ein strukturiertes KI-Ergebnis und prüft den erkannten Fragenentwurf.
    /// </summary>
    /// <param name="raw">Das unverarbeitete strukturierte KI-Ergebnis.</param>
    /// <param name="mediaId">Die Kennung des privaten Mediums.</param>
    /// <param name="mode">Der ausgewählte KI-Betriebsmodus.</param>
    /// <param name="referenceHint">Der ergänzende Hinweis zur Referenzlösung.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static PhotoReview? Parse(
        string raw,
        Guid mediaId,
        string mode,
        string? referenceHint)
    {
        PhotoRecognition? item;
        try
        {
            item = JsonSerializer.Deserialize<PhotoRecognition>(raw, Json);
        }
        catch (JsonException)
        {
            return null;
        }

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
            item.SuggestedCorrectIndex >= item.Answers.Count)
        {
            return null;
        }

        var reference = string.IsNullOrWhiteSpace(referenceHint) ? item.ReferenceSolution :
            referenceHint.Trim();
        var comparison = "unknown";
        if (!string.IsNullOrWhiteSpace(reference))
        {
            comparison = Normalize(reference) == Normalize(item.ComputedSolution) ? "same-text" : "different-text";
        }

        return new PhotoReview(
            item with
            {
                ReferenceSolution = reference,
                Uncertainties = item.Uncertainties.Append(
                "KI-Erkennung und Lösung sind ungeprüft; Formeln und Zeichnungen kontrollieren.")
                .Distinct().ToArray(),
            },
            comparison,
            comparison == "unknown" ? "Keine Musterlösung aus der Vorlage verfügbar." : "Nur Textvergleich mit der Vorlage; mathematische Gleichwertigkeit und Richtigkeit " + "wurden nicht bewiesen.",
            mediaId,
            mode,
            SolutionVerifier.Check(
                item.Formula,
                item.ComputedSolution,
                reference,
                item.SuggestedCorrectIndex is int index ? item.Answers[index] : null,
                item.Steps,
                item.QuestionText));
    }

    private static bool Valid(string? text, int max) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= max;

    private static string Normalize(string text) =>
        string.Concat(text.Where(character => !char.IsWhiteSpace(character)))
            .ToUpperInvariant();
}
