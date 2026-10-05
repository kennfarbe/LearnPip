// <copyright file="CatalogPackageWriter.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Erzeugt deterministische Offline-Pakete und prüft sie mit dem produktiven Reader.</summary>
public static class CatalogPackageWriter
{
    /// <summary>Erzeugt ausschließlich Inhaltsdaten, ohne Konten, Lernstände oder Veröffentlichung.</summary>
    /// <param name="title">Der ausdrücklich gewählte Titel.</param>
    /// <param name="publisher">Der ausdrücklich gewählte Herausgeber.</param>
    /// <param name="questions">Vollständig ausgezeichnete Fragen im Entwurfsformat 0.2.0.</param>
    /// <param name="media">Nur tatsächlich referenzierte Originalmedien.</param>
    /// <param name="notices">Die vollständigen Lizenz-, Quellen- und Attributionsnachweise.</param>
    /// <param name="createdAt">Der feste Inhaltsstand für reproduzierbare Downloads.</param>
    /// <returns>Ein vollständig validiertes, importierbares Paket.</returns>
    public static CatalogPackage Write(string title, string publisher, IReadOnlyList<JsonObject> questions, IReadOnlyDictionary<string, byte[]> media, IReadOnlyDictionary<string, string> notices, DateTimeOffset createdAt)
    {
        var ordered = questions.OrderBy(question => question["id"]!.GetValue<string>(), StringComparer.Ordinal).ToArray();
        var ids = string.Join("\n", ordered.Select(question => question["id"]!.GetValue<string>()));
        var files = new SortedDictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["questions.json"] = JsonSerializer.SerializeToUtf8Bytes(new { questions = ordered }),
        };
        foreach (var (path, data) in media)
        {
            files.Add(path, data);
        }

        foreach (var (path, text) in notices)
        {
            files.Add(path, Encoding.UTF8.GetBytes(text));
        }

        if (files.Count > 2000 || files.Values.Sum(data => (long)data.Length) > 100L * 1024 * 1024)
        {
            throw new InvalidDataException("Die Auswahl überschreitet die Paketgrenzen. Bitte weniger Fragen auswählen.");
        }

        var contentHash = Hash(files["questions.json"]);
        var manifest = new JsonObject
        {
            ["format_id"] = "org.learnpip.catalog.zip",
            ["schema_version"] = "0.2.0",
            ["package_id"] = "selection." + Hash(Encoding.UTF8.GetBytes(ids)),
            ["catalog_version"] = contentHash,
            ["source_revision"] = contentHash,
            ["title"] = title,
            ["description"] = "Lokale Auswahl von Frageinhalten; keine öffentliche Freigabe.",
            ["language"] = ordered[0]["language"]!.DeepClone(),
            ["publisher"] = publisher,
            ["created_at"] = createdAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ["license"] = new JsonObject
            {
                ["id"] = "LicenseRef-Collection",
                ["holder"] = publisher,
                ["attribution"] = "Die Einzellizenzen der Fragen und Medien sind maßgeblich; siehe LICENSES.md, NOTICE und ATTRIBUTION.",
            },
            ["files"] = new JsonArray(files.Select(file => (JsonNode)new JsonObject
            {
                ["path"] = file.Key,
                ["size"] = file.Value.Length,
                ["sha256"] = Hash(file.Value),
            }).ToArray()),
        };
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "manifest.json", JsonSerializer.SerializeToUtf8Bytes(manifest));
            foreach (var (path, bytes) in files)
            {
                AddEntry(archive, path, bytes);
                if (buffer.Length > CatalogPackageReader.MaxArchiveBytes)
                {
                    throw new InvalidDataException("Das ZIP überschreitet 25 MiB. Bitte weniger Fragen auswählen.");
                }
            }
        }

        var result = CatalogPackageReader.Read(buffer.ToArray());
        CatalogPackageImporter.PrepareImages(result);
        return result;
    }

    private static void AddEntry(ZipArchive archive, string path, byte[] bytes)
    {
        // NoCompression prevents highly repetitive authored text from exceeding the reader's ratio limit.
        var entry = archive.CreateEntry(path, CompressionLevel.NoCompression);
        entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
