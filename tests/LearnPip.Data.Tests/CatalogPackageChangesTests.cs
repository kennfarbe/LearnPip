// <copyright file="CatalogPackageChangesTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;
using LearnPip.Api.CatalogPackages;

namespace LearnPip.Data.Tests;

/// <summary>Prüft Vorschauunterschiede einschließlich Rechte und Originalbildbytes.</summary>
public sealed class CatalogPackageChangesTests
{
    /// <summary>Erkennt Neuimport und identische Inhalte trotz anderer ZIP-Formatierung.</summary>
    [Fact]
    public void RepackingDoesNotChangeQuestions()
    {
        var original = CatalogPackageReader.Read(CatalogPackageReaderTests.Golden());
        var repacked = CatalogPackageReader.Read(CatalogPackageReaderTests.Rewrite(null, null));
        Assert.Equal(new CatalogPackageChanges(1, 0, 0, 0), CatalogPackageChanges.Compare(null, original));
        Assert.Equal(new CatalogPackageChanges(0, 0, 0, 1), CatalogPackageChanges.Compare(original, repacked));
    }

    /// <summary>Trennt entfallene Kennungen von neuen Fragen statt sie als Änderung zu zählen.</summary>
    [Fact]
    public void ReplacedIdentifierCountsAsRemovedAndNew()
    {
        var original = CatalogPackageReader.Read(CatalogPackageReaderTests.Golden());
        var node = JsonNode.Parse(original.Questions[0].GetRawText())!;
        node["id"] = "synthetic:new-id";
        var changed = original with { Questions = [JsonSerializer.SerializeToElement(node)] };
        Assert.Equal(new CatalogPackageChanges(1, 1, 0, 0), CatalogPackageChanges.Compare(original, changed));
        Assert.Equal(new CatalogPackageChanges(0, 1, 0, 0), CatalogPackageChanges.Compare(original, original with { Questions = [] }));
    }

    /// <summary>Behandelt umbenannte Bilddateien mit identischen Nachweisen als unverändert.</summary>
    [Fact]
    public void RenamedMediaKeepsItsMeaning()
    {
        var original = CatalogPackageReader.Read(CatalogPackageReaderTests.Golden());
        var node = JsonNode.Parse(original.Questions[0].GetRawText())!;
        var oldPath = node["media"]![0]!["path"]!.GetValue<string>();
        const string newPath = "media/renamed.png";
        node["media"]![0]!["path"] = newPath;
        var files = original.Files.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        files[newPath] = files[oldPath];
        files.Remove(oldPath);
        var renamed = original with { Questions = [JsonSerializer.SerializeToElement(node)], Files = files };
        Assert.Equal(new CatalogPackageChanges(0, 0, 0, 1), CatalogPackageChanges.Compare(original, renamed));
    }

    /// <summary>Bezieht alle wesentlichen Frageangaben und Medien in den Vergleich ein.</summary>
    /// <param name="field">Die synthetische Inhaltsänderung.</param>
    [Theory]
    [InlineData("prompt")]
    [InlineData("answer")]
    [InlineData("correct")]
    [InlineData("license")]
    [InlineData("source")]
    [InlineData("alt")]
    [InlineData("bytes")]
    public void ContentAndRightsChangesAreReported(string field)
    {
        var original = CatalogPackageReader.Read(CatalogPackageReaderTests.Golden());
        var node = JsonNode.Parse(original.Questions[0].GetRawText())!;
        var files = original.Files.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        switch (field)
        {
            case "prompt": node["prompt"] = "Geänderte Frage"; break;
            case "answer": node["answers"]![0]!["text"] = "Geänderte Antwort"; break;
            case "correct": node["correct_answer_ids"] = new JsonArray("a"); break;
            case "license": node["media"]![0]!["license"]!["holder"] = "Anderer Rechteinhaber"; break;
            case "source": node["provenance"]!["source_revision"] = "Anderer Quellenstand"; break;
            case "alt": node["media"]![0]!["alt"] = "Andere Bildbeschreibung"; break;
            case "bytes": files[node["media"]![0]!["path"]!.GetValue<string>()] = [1, 2, 3]; break;
        }

        var changed = original with { Questions = [JsonSerializer.SerializeToElement(node)], Files = files };
        Assert.Equal(new CatalogPackageChanges(0, 0, 1, 0), CatalogPackageChanges.Compare(original, changed));
    }
}
