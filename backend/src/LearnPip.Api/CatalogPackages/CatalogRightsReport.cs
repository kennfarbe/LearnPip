// <copyright file="CatalogRightsReport.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;
using LearnPip.Api.Questions;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Prüft Community-Mindestnachweise ohne rechtliche Rechteprüfung vorzutäuschen.</summary>
public static class CatalogRightsReport
{
    /// <summary>Ermittelt separat für Text und jedes Asset offene Nachweislücken.</summary>
    /// <param name="questions">Die tatsächlich exportierten Fragen.</param>
    /// <returns>Ein Bericht, der private Exporte nicht pauschal sperrt.</returns>
    public static IReadOnlyList<string> Inspect(IReadOnlyList<JsonElement> questions)
    {
        var issues = new List<string>();
        foreach (var question in questions)
        {
            var id = question.GetProperty("id").GetString()!;
            Check(question, id + " / Text", issues, true);
            foreach (var image in question.GetProperty("media").EnumerateArray())
            {
                Check(image, id + " / " + image.GetProperty("path").GetString(), issues, false);
            }
        }

        return issues;
    }

    /// <summary>Prüft gespeicherte Nachweise gegen genau die einzureichende Fassung.</summary>
    /// <param name="questionId">Die Quellfrage.</param>
    /// <param name="version">Die unveränderliche Veröffentlichungskandidatin.</param>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Die vollständigen technischen Sperrgründe.</returns>
    public static async Task<IReadOnlyList<string>> ForVersion(Guid questionId, PublishedQuestionVersion version, LearnPipDbContext db, CancellationToken ct)
    {
        var content = CatalogRightsEndpoints.VersionContent(version);
        var evidence = await Evidence(questionId, version, db, ct);
        if (evidence == null)
        {
            return ["Vollständige aktuelle Einzelnachweise fehlen. Unter Quellen und Rechte je Frage und Bild prüfen und speichern; anschließend neue Vorschau laden."];
        }

        var rights = JsonNode.Parse(evidence)!.AsObject();
        var assets = rights["media"]!.AsObject();
        var ids = CatalogRightsEndpoints.ImageIds(content);
        if (assets.Count != ids.Length || ids.Any(id => !assets.ContainsKey(id.ToString())))
        {
            return ["Einzelne Bildnachweise fehlen."];
        }

        var question = new JsonObject
        {
            ["id"] = questionId.ToString(),
            ["source_note"] = version.Source,
            ["license"] = rights["license"]!.DeepClone(),
            ["provenance"] = rights["provenance"]!.DeepClone(),
            ["media"] = new JsonArray(assets.Select(asset =>
            {
                var item = asset.Value!.DeepClone().AsObject();
                item["path"] = asset.Key;
                return (JsonNode)item;
            }).ToArray()),
        };
        var report = Inspect([JsonSerializer.SerializeToElement(question)]).ToList();
        if (!string.IsNullOrWhiteSpace(version.License) && Normalize(version.License) != Normalize(rights["license"]!["id"]!.GetValue<string>()))
        {
            report.Add("Die gespeicherte Quelllizenz darf nicht durch die Einreichung ersetzt werden.");
        }

        return report;
    }

    /// <summary>Vergleicht gebräuchliche Schreibweisen derselben Inhaltslizenz.</summary>
    /// <param name="license">Die ausdrücklich angegebene Lizenz.</param>
    /// <returns>Die kanonische Vergleichsschreibweise ohne neue Lizenzzuweisung.</returns>
    public static string Normalize(string license) => license switch
    {
        "CC BY 4.0" => "CC-BY-4.0",
        "CC BY-SA 4.0" => "CC-BY-SA-4.0",
        "CC0 1.0" => "CC0-1.0",
        _ => license,
    };

    /// <summary>Liest genau die zur Fassung bestätigten aktuellen oder historischen Nachweise.</summary>
    /// <param name="questionId">Die eigene Quellfrage.</param>
    /// <param name="version">Die unveränderliche Inhaltsfassung.</param>
    /// <param name="db">Der Datenbankkontext.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Die ursprünglichen Nachweise oder kein passender bestätigter Stand.</returns>
    internal static async Task<string?> Evidence(Guid questionId, PublishedQuestionVersion version, LearnPipDbContext db, CancellationToken ct)
    {
        var saved = await db.QuestionRights.AsNoTracking().SingleOrDefaultAsync(item => item.QuestionId == questionId, ct);
        if (saved == null)
        {
            return null;
        }

        var hash = await CatalogRightsEndpoints.Fingerprint(CatalogRightsEndpoints.VersionContent(version), db, ct);
        if (saved.ContentSha256 == hash)
        {
            return saved.PayloadJson;
        }

        var previous = JsonSerializer.Deserialize<JsonElement>(saved.HistoryJson).EnumerateArray()
            .FirstOrDefault(entry => entry.GetProperty("contentSha256").GetString() == hash);
        return previous.ValueKind == JsonValueKind.Undefined ? null : previous.GetProperty("rights").GetRawText();
    }

    private static void Check(JsonElement content, string label, List<string> issues, bool isQuestionText)
    {
        var license = content.GetProperty("license");
        var provenance = content.GetProperty("provenance");
        var id = Normalize(license.GetProperty("id").GetString()!);
        if (id is not ("CC-BY-SA-4.0" or "CC-BY-4.0" or "CC0-1.0" or "DL-DE/BY-2.0" or "dl-de/by-2-0"))
        {
            issues.Add(label + ": Keine unterstützte offene Inhaltslizenz. Private Nutzung kann weiterhin zulässig sein; Rechte klären oder zulässige offene Lizenz ausdrücklich wählen.");
        }

        if (string.IsNullOrWhiteSpace(license.GetProperty("holder").GetString()) || string.IsNullOrWhiteSpace(license.GetProperty("attribution").GetString()))
        {
            issues.Add(label + ": Rechteinhaber oder Attribution fehlt.");
        }

        var kind = provenance.GetProperty("kind").GetString();
        if (kind == "original" && provenance.TryGetProperty("source_url", out _))
        {
            issues.Add(label + ": Originalangabe mit Drittquelle widersprüchlich. Herkunft und gemischte Quellen einzeln abgrenzen.");
        }

        if (content.TryGetProperty("source_note", out var sourceNote) && Uri.TryCreate(sourceNote.GetString(), UriKind.Absolute, out var actualSource) && actualSource.Scheme is "http" or "https" &&
            (kind == "original" || !provenance.TryGetProperty("source_url", out var declaredSource) || declaredSource.GetString() != actualSource.AbsoluteUri))
        {
            issues.Add(label + ": Die gespeicherte Drittquelle benötigt einen passenden Herkunftsnachweis; keine Kennzeichnung als eigenes Original.");
        }

        if (provenance.TryGetProperty("source_url", out var source) && Uri.TryCreate(source.GetString(), UriKind.Absolute, out var uri))
        {
            if (isQuestionText && (uri.Host == "wikipedia.org" || uri.Host.EndsWith(".wikipedia.org", StringComparison.OrdinalIgnoreCase)) && id != "CC-BY-SA-4.0")
            {
                issues.Add(label + ": Wikipedia-Nachweis benötigt CC BY-SA 4.0 und getrennte Einzellizenzen für Bilder; keine automatische Umlizenzierung.");
            }

            if (isQuestionText && (uri.Host == "bundesnetzagentur.de" || uri.Host.EndsWith(".bundesnetzagentur.de", StringComparison.OrdinalIgnoreCase)) && id is not ("DL-DE/BY-2.0" or "dl-de/by-2-0"))
            {
                issues.Add(label + ": Amtliche Originaldaten benötigen den tatsächlichen DL-DE/BY-2.0-Nachweis; Bearbeitungen und fremde Assets getrennt prüfen.");
            }
        }

        if (kind != "original" && (!provenance.TryGetProperty("source_revision", out var revision) || string.IsNullOrWhiteSpace(revision.GetString())))
        {
            issues.Add(label + ": Quellenrevision fehlt.");
        }

        if (kind == "adapted" && (!provenance.TryGetProperty("modification_note", out var note) || string.IsNullOrWhiteSpace(note.GetString())))
        {
            issues.Add(label + ": Bearbeitungsvermerk fehlt. Gemischte Inhalte und ihre Lizenzabgrenzung dokumentieren.");
        }
    }
}
