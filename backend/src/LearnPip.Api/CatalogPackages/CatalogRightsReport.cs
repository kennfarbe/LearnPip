// <copyright file="CatalogRightsReport.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;

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
            Check(question, id + " / Text", issues);
            foreach (var image in question.GetProperty("media").EnumerateArray())
            {
                Check(image, id + " / " + image.GetProperty("path").GetString(), issues);
            }
        }

        return issues;
    }

    private static void Check(JsonElement content, string label, List<string> issues)
    {
        var license = content.GetProperty("license");
        var provenance = content.GetProperty("provenance");
        var id = license.GetProperty("id").GetString();
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

        if (provenance.TryGetProperty("source_url", out var source) && Uri.TryCreate(source.GetString(), UriKind.Absolute, out var uri))
        {
            if ((uri.Host == "wikipedia.org" || uri.Host.EndsWith(".wikipedia.org", StringComparison.OrdinalIgnoreCase)) && id != "CC-BY-SA-4.0")
            {
                issues.Add(label + ": Wikipedia-Nachweis benötigt CC BY-SA 4.0 und getrennte Einzellizenzen für Bilder; keine automatische Umlizenzierung.");
            }

            if ((uri.Host == "bundesnetzagentur.de" || uri.Host.EndsWith(".bundesnetzagentur.de", StringComparison.OrdinalIgnoreCase)) && id is not ("DL-DE/BY-2.0" or "dl-de/by-2-0"))
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
