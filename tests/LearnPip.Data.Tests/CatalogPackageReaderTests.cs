// <copyright file="CatalogPackageReaderTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using LearnPip.Api.CatalogPackages;

namespace LearnPip.Data.Tests;

/// <summary>Prüft den produktiven Reader am dauerhaft archivierten Offline-Paket.</summary>
public sealed class CatalogPackageReaderTests
{
    /// <summary>Lädt das echte archivierte ZIP, ohne es aus Testhelfern neu zu erzeugen.</summary>
    /// <returns>Die Originaldatei.</returns>
    public static byte[] Golden() => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "catalog-golden.zip"));

    /// <summary>Erzeugt ausschließlich synthetische Testvarianten mit passenden Integritätswerten.</summary>
    /// <param name="mutation">Die gezielte Vertragsverletzung.</param>
    /// <param name="prompt">Ein gültiger geänderter Fragetext.</param>
    /// <returns>Ein synthetisches ZIP.</returns>
    public static byte[] Rewrite(string? mutation, string? prompt)
    {
        using var input = new MemoryStream(Golden());
        using var source = new ZipArchive(input, ZipArchiveMode.Read);
        var files = source.Entries.ToDictionary(
            entry => entry.FullName,
            entry =>
        {
            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        },
            StringComparer.Ordinal);
        var manifest = JsonNode.Parse(files["manifest.json"])!;
        var questions = JsonNode.Parse(files["questions.json"])!;
        if (prompt != null)
        {
            questions["questions"]![0]!["prompt"] = prompt;
        }

        switch (mutation)
        {
            case "version": manifest["schema_version"] = "99.0.0"; break;
            case "unicode": manifest["title"] = string.Concat(Enumerable.Repeat("😀", 256)); break;
            case "unknown": manifest["account_secret"] = "synthetic"; break;
            case "answer": questions["questions"]![0]!["correct_answer_ids"] = new JsonArray("missing"); break;
            case "license": questions["questions"]![0]!["media"]![0]!["license"]!.AsObject().Remove("holder"); break;
        }

        files["questions.json"] = System.Text.Encoding.UTF8.GetBytes(questions.ToJsonString());
        foreach (var record in manifest["files"]!.AsArray())
        {
            var path = record!["path"]!.GetValue<string>();
            record["size"] = files[path].Length;
            record["sha256"] = Convert.ToHexStringLower(SHA256.HashData(files[path]));
        }

        if (mutation == "size")
        {
            manifest["files"]![0]!["size"] = "not a number";
        }

        if (mutation == "checksum")
        {
            files["NOTICE"] = "tampered"u8.ToArray();
        }

        files["manifest.json"] = System.Text.Encoding.UTF8.GetBytes(manifest.ToJsonString());
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, data) in files)
            {
                using var entry = archive.CreateEntry(name, CompressionLevel.Fastest).Open();
                entry.Write(data);
            }

            if (mutation is "duplicate" or "path")
            {
                using var entry = archive.CreateEntry(mutation == "duplicate" ? "NOTICE" : "../escape").Open();
                entry.WriteByte(1);
            }
        }

        return output.ToArray();
    }

    /// <summary>Prüft jeden unveränderten Archivstand mit dem aktuellen produktiven Reader.</summary>
    /// <param name="filename">Der dauerhaft archivierte Vertragsbestand.</param>
    /// <param name="version">Die ausdrücklich unterstützte Formatversion.</param>
    [Theory]
    [InlineData("catalog-golden.zip", "0.1.0")]
    [InlineData("catalog-blocks-golden.zip", "0.2.0")]
    [InlineData("catalog-stable-golden.zip", "1.0.0")]
    public void EveryArchivedContractRemainsReadable(string filename, string version)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", filename));
        var package = CatalogPackageReader.Read(bytes);
        Assert.Equal(version, package.Manifest.GetProperty("schema_version").GetString());
        Assert.Equal(bytes, package.Archive);
        Assert.Single(CatalogPackageImporter.PrepareImages(package));
        var question = Assert.Single(package.Questions);
        Assert.Equal("CC-BY-SA-4.0", question.GetProperty("license").GetProperty("id").GetString());
        Assert.Equal("CC-BY-4.0", question.GetProperty("media")[0].GetProperty("license").GetProperty("id").GetString());
    }

    /// <summary>Wertet JSON-Schema-Längen als Unicode-Zeichen statt UTF-16-Codeeinheiten aus.</summary>
    [Fact]
    public void SchemaTextBoundariesCountUnicodeScalars()
    {
        var package = CatalogPackageReader.Read(Rewrite("unicode", null));
        Assert.Equal(string.Concat(Enumerable.Repeat("😀", 256)), package.Manifest.GetProperty("title").GetString());
    }

    /// <summary>Verhindert Teilimporte bei Modellgrenzen und nicht speicherbaren Nullzeichen.</summary>
    /// <param name="kind">Die synthetische Darstellungsgrenze.</param>
    [Theory]
    [InlineData("long")]
    [InlineData("null")]
    public void UndisplayableContentIsRejectedBeforeWriting(string kind)
    {
        var prompt = kind == "long" ? new string('a', 4001) : "synthetic\0question";
        var package = CatalogPackageReader.Read(Rewrite(null, prompt));
        Assert.Throws<InvalidDataException>(() => CatalogPackageImporter.PrepareImages(package));
    }

    /// <summary>Prüft Originalbytes, Bilddarstellung und getrennte Lizenznachweise.</summary>
    [Fact]
    public void GoldenArchiveRetainsOriginalContentAndCanBeDisplayed()
    {
        var bytes = Golden();
        var package = CatalogPackageReader.Read(bytes);
        Assert.Equal(bytes, package.Archive);
        Assert.Equal("example.provenance", package.Manifest.GetProperty("package_id").GetString());
        var question = Assert.Single(package.Questions);
        Assert.Equal("CC-BY-SA-4.0", question.GetProperty("license").GetProperty("id").GetString());
        Assert.Equal("CC-BY-4.0", question.GetProperty("media")[0].GetProperty("license").GetProperty("id").GetString());
        Assert.Equal("b", question.GetProperty("correct_answer_ids")[0].GetString());
        Assert.Single(CatalogPackageImporter.PrepareImages(package));
    }

    /// <summary>Prüft, dass andere JSON-Formatierung und ZIP-Kompression keine Duplikate erzeugen.</summary>
    [Fact]
    public void RepackedJsonHasTheSameSemanticFingerprint()
    {
        var original = CatalogPackageReader.Read(Golden());
        var repacked = Rewrite(null, null);
        Assert.NotEqual(original.Archive, repacked);
        Assert.Equal(original.Fingerprint, CatalogPackageReader.Read(repacked).Fingerprint);
    }

    /// <summary>Prüft Schemafelder, Versionsangaben, Antworten und Integrität vor jeglichem Import.</summary>
    /// <param name="mutation">Die synthetische Paketmanipulation.</param>
    [Theory]
    [InlineData("version")]
    [InlineData("unknown")]
    [InlineData("size")]
    [InlineData("answer")]
    [InlineData("license")]
    [InlineData("checksum")]
    [InlineData("duplicate")]
    [InlineData("path")]
    public void MalformedPackagesAreRejected(string mutation)
    {
        var bytes = Rewrite(mutation, null);
        Assert.Throws<InvalidDataException>(() => CatalogPackageReader.Read(bytes));
    }

    /// <summary>Prüft, dass gültige Inhaltsänderungen als Konflikt erkennbar bleiben.</summary>
    [Fact]
    public void ChangedQuestionHasDifferentFingerprint()
    {
        Assert.NotEqual(
            CatalogPackageReader.Read(Golden()).Fingerprint,
            CatalogPackageReader.Read(Rewrite(null, "Geänderte synthetische Frage")).Fingerprint);
    }
}
