// <copyright file="CatalogPackageWriterTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LearnPip.Api.CatalogPackages;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data.Tests;

/// <summary>Prüft Blockreihenfolge, Einzellizenzen und deterministische ZIPs beider Entwurfsstände.</summary>
public sealed class CatalogPackageWriterTests
{
    /// <summary>Lädt das tatsächlich archivierte Nachfolgeformat, ohne es im Test neu zu erzeugen.</summary>
    /// <returns>Das synthetische Archiv mit Antwort- und Erklärungsbildern.</returns>
    public static CatalogPackage BlocksGolden() => CatalogPackageReader.Read(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "catalog-blocks-golden.zip")));

    /// <summary>Prüft Bytes, Metadaten, Reihenfolge und unveränderte Originaldatei.</summary>
    [Fact]
    public void DeterministicWriterPreservesBlocksAndSeparateLicenses()
    {
        var source = BlocksGolden();
        var question = JsonNode.Parse(Assert.Single(source.Questions).GetRawText())!.AsObject();
        var media = source.Files.Where(file => file.Key.StartsWith("media/", StringComparison.Ordinal)).ToDictionary(file => file.Key, file => file.Value, StringComparer.Ordinal);
        var notices = source.Files.Where(file => file.Key is "LICENSES.md" or "NOTICE" or "ATTRIBUTION").ToDictionary(file => file.Key, file => Encoding.UTF8.GetString(file.Value), StringComparer.Ordinal);
        var date = new DateTimeOffset(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);
        var first = CatalogPackageWriter.Write("Synthetischer Export", "Testautor", [question], media, notices, date);
        var second = CatalogPackageWriter.Write("Synthetischer Export", "Testautor", [question], media, notices, date);
        Assert.Equal(first.Archive, second.Archive);
        Assert.Equal(question.ToJsonString(), JsonNode.Parse(Assert.Single(first.Questions).GetRawText())!.ToJsonString());
        Assert.Equal(source.Files["media/diagram.png"], first.Files["media/diagram.png"]);
        Assert.Equal("image", first.Questions[0].GetProperty("answers")[0].GetProperty("blocks")[0].GetProperty("kind").GetString());
        Assert.Equal("CC-BY-SA-4.0", first.Questions[0].GetProperty("license").GetProperty("id").GetString());
        Assert.Equal("CC-BY-4.0", first.Questions[0].GetProperty("media")[0].GetProperty("license").GetProperty("id").GetString());
        Assert.Equal(source.Archive, BlocksGolden().Archive);
    }

    /// <summary>Prüft die tatsächlich erzeugten lokalen Daten ohne Datenbankspeicherung.</summary>
    [Fact]
    public void ImportRestoresAnswerAndExplanationImagesInOrder()
    {
        var package = BlocksGolden();
        using var db = new LearnPipDbContext(new DbContextOptionsBuilder<LearnPipDbContext>().UseNpgsql("Host=localhost;Database=unused").Options);
        var owner = Guid.NewGuid();
        var catalog = new PrivateCatalog { OwnerAccountId = owner, Name = "Synthetisch" };
        var mapping = CatalogPackageImporter.AddQuestions(db, package, CatalogPackageImporter.PrepareImages(package), owner, catalog);
        var source = Assert.Single(package.Questions);
        Assert.Equal(source.GetProperty("id").GetString(), Assert.Single(mapping).Key);
        var version = Assert.Single(db.QuestionVersions.Local);
        Assert.Equal("Technik", version.Subject);
        Assert.Equal("Bildantworten", version.Topic);
        Assert.Equal("Synthetischer Herkunftshinweis", version.Source);
        var explanation = db.QuestionContentBlocks.Local.Where(block => block.Section == "explanation").OrderBy(block => block.SortOrder).ToArray();
        Assert.Equal(["text", "image"], explanation.Select(block => block.Kind).ToArray());
        var answer = db.AnswerOptions.Local.Single(option => option.SortOrder == 0);
        Assert.Equal(["image", "text"], db.QuestionContentBlocks.Local.Where(block => block.AnswerOptionId == answer.Id).OrderBy(block => block.SortOrder).Select(block => block.Kind).ToArray());
        Assert.Equal("private", version.Visibility);
        Assert.Empty(db.StudyAttempts.Local);
    }

    /// <summary>Verweigert falsch zugeordnete Blöcke und inkonsistente Zusammenfassungen.</summary>
    /// <param name="mutation">Die gezielte synthetische Manipulation.</param>
    [Theory]
    [InlineData("reference")]
    [InlineData("summary")]
    [InlineData("mode")]
    [InlineData("extra")]
    public void InvalidBlockContractsCannotBeExported(string mutation)
    {
        var source = BlocksGolden();
        var question = JsonNode.Parse(source.Questions[0].GetRawText())!.AsObject();
        switch (mutation)
        {
            case "reference": question["answers"]![0]!["blocks"]![0]!["path"] = "media/missing.png"; break;
            case "summary": question["prompt"] = "Falsche Zusammenfassung"; break;
            case "mode": question["selection_mode"] = "multiple"; break;
            case "extra": question["prompt_blocks"]![0]!["account"] = "not allowed"; break;
        }

        var media = source.Files.Where(file => file.Key.StartsWith("media/", StringComparison.Ordinal)).ToDictionary(file => file.Key, file => file.Value, StringComparer.Ordinal);
        var notices = source.Files.Where(file => file.Key is "LICENSES.md" or "NOTICE" or "ATTRIBUTION").ToDictionary(file => file.Key, file => Encoding.UTF8.GetString(file.Value), StringComparer.Ordinal);
        Assert.Throws<InvalidDataException>(() => CatalogPackageWriter.Write("Synthetisch", "Testautor", [question], media, notices, DateTimeOffset.UnixEpoch));
    }
}
