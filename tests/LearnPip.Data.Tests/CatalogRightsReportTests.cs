// <copyright file="CatalogRightsReportTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using LearnPip.Api.CatalogPackages;

namespace LearnPip.Data.Tests;

/// <summary>Prüft Rechteberichte unabhängig von Datenbank oder Netzwerk.</summary>
public sealed class CatalogRightsReportTests
{
    /// <summary>Offene Originale, Wikipedia-Bearbeitungen und amtliche Quellen bleiben getrennt.</summary>
    /// <param name="license">Die tatsächlich gewählte Lizenz.</param>
    /// <param name="source">Der tatsächliche Quellnachweis.</param>
    /// <param name="kind">Die Herkunftsart.</param>
    /// <param name="blocked">Ob ein Community-Nachweis fehlt.</param>
    [Theory]
    [InlineData("CC-BY-SA-4.0", "", "original", false)]
    [InlineData("LicenseRef-Private", "", "original", true)]
    [InlineData("CC-BY-SA-4.0", "https://de.wikipedia.org/w/index.php?oldid=42", "adapted", false)]
    [InlineData("AGPL-3.0-only", "https://de.wikipedia.org/wiki/Test", "adapted", true)]
    [InlineData("DL-DE/BY-2.0", "https://www.bundesnetzagentur.de/test", "verbatim", false)]
    [InlineData("CC-BY-SA-4.0", "https://www.bundesnetzagentur.de/test", "verbatim", true)]
    public void CommunityRequiresActualOpenIndividualEvidence(string license, string source, string kind, bool blocked)
    {
        var provenance = new Dictionary<string, string> { ["kind"] = kind };
        if (source.Length != 0)
        {
            provenance["source_url"] = source;
            provenance["source_revision"] = "synthetic-revision-42";
            provenance["modification_note"] = "Eigene Bearbeitung dokumentiert";
        }

        var question = JsonSerializer.SerializeToElement(new { id = "synthetic", license = new { id = license, holder = "Testautor", attribution = "Synthetisch" }, provenance, media = Array.Empty<object>() });
        Assert.Equal(blocked, CatalogRightsReport.Inspect([question]).Count != 0);
    }

    /// <summary>Eine tatsächliche Drittquelle darf nicht durch eine Originalbehauptung verdeckt werden.</summary>
    [Fact]
    public void ActualSourceCannotBeRelabeledAsOriginal()
    {
        var question = JsonSerializer.SerializeToElement(new
        {
            id = "synthetic",
            source_note = "https://de.wikipedia.org/wiki/Synthetic",
            license = new { id = "CC-BY-4.0", holder = "Testautor", attribution = "Synthetisch" },
            provenance = new { kind = "original" },
            media = Array.Empty<object>(),
        });
        Assert.Single(CatalogRightsReport.Inspect([question]));
    }

    /// <summary>Einzeln belegte Bildlizenzen werden nicht aus der Lizenz des Quelltexts abgeleitet.</summary>
    /// <param name="source">Die tatsächliche Bildquelle.</param>
    [Theory]
    [InlineData("https://de.wikipedia.org/wiki/Synthetic")]
    [InlineData("https://www.bundesnetzagentur.de/synthetic")]
    public void ImageLicenseRemainsIndependentOfSourceTextLicense(string source)
    {
        var question = JsonSerializer.SerializeToElement(new
        {
            id = "synthetic",
            license = new { id = "CC-BY-SA-4.0", holder = "Textautor", attribution = "Originalfrage" },
            provenance = new { kind = "original" },
            media = new[]
            {
                new
                {
                    path = "media/test.png",
                    license = new { id = "CC-BY-4.0", holder = "Bildautor", attribution = "Getrennter synthetischer Bildnachweis" },
                    provenance = new { kind = "verbatim", source_url = source, source_revision = "synthetic-image-revision-1" },
                },
            },
        });
        Assert.Empty(CatalogRightsReport.Inspect([question]));
    }

    /// <summary>Eine offene Textlizenz darf ein privat lizenziertes Bild nicht verdecken.</summary>
    [Fact]
    public void MixedMediaAreCheckedIndependently()
    {
        var original = new { kind = "original" };
        var question = JsonSerializer.SerializeToElement(new
        {
            id = "synthetic",
            license = new { id = "CC-BY-SA-4.0", holder = "Textautor", attribution = "Originalfrage" },
            provenance = original,
            media = new[] { new { path = "media/test.png", license = new { id = "LicenseRef-Private", holder = "Bildautor", attribution = "Private Zeichnung" }, provenance = original } },
        });
        var report = CatalogRightsReport.Inspect([question]);
        Assert.Single(report);
        Assert.Contains("media/test.png", report[0]);
    }
}
