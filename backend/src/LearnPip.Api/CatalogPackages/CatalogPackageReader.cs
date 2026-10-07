// <copyright file="CatalogPackageReader.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Prüft ZIP-Sicherheit, Integrität und den archivierten Formatvertrag 0.1.0 offline.</summary>
public static partial class CatalogPackageReader
{
    /// <summary>Maximale Größe eines komprimierten Pakets.</summary>
    public const int MaxArchiveBytes = 25 * 1024 * 1024;
    private const int MaxFileBytes = 20 * 1024 * 1024;
    private static readonly HashSet<string> RequiredFiles = ["questions.json", "LICENSES.md", "NOTICE", "ATTRIBUTION"];
    private static readonly HashSet<string> MediaExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg", ".txt", ".pdf"];

    /// <summary>Liest erst nach vollständiger Validierung ohne Dateiextraktion ein Paket.</summary>
    /// <param name="bytes">Die komprimierten Originalbytes.</param>
    /// <returns>Ein vollständig validiertes Paket.</returns>
    /// <exception cref="InvalidDataException">Das Paket ist ungültig oder inkompatibel.</exception>
    public static CatalogPackage Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        Require(bytes.Length is > 0 and <= MaxArchiveBytes, "Paket ist leer oder größer als 25 MiB.");
        try
        {
            using var buffer = new MemoryStream(bytes, writable: false);
            using var zip = new ZipArchive(buffer, ZipArchiveMode.Read);
            var files = ReadFiles(zip);
            Require(RequiredFiles.IsSubsetOf(files.Keys), "Pflichtdateien fehlen.");
            var manifest = Parse(files["manifest.json"]);
            ValidateManifest(manifest, files);
            var questionsRoot = Parse(files["questions.json"]);
            Fields(questionsRoot, "questions", "questions");
            var questions = Array(questionsRoot, "questions", 1, 10000);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var media = new HashSet<string>(StringComparer.Ordinal);
            foreach (var question in questions)
            {
                ValidateQuestion(question, files, ids, media, manifest.GetProperty("schema_version").GetString() == "0.2.0");
            }

            Require(
                media.SetEquals(files.Keys.Where(path => path.StartsWith("media/", StringComparison.Ordinal))),
                "Medienverweise und enthaltene Dateien stimmen nicht überein.");
            return new CatalogPackage(manifest, questions, files, bytes, Fingerprint(manifest, files));
        }
        catch (Exception error) when (error is JsonException or FormatException or OverflowException)
        {
            throw new InvalidDataException("Ungültige JSON-Daten im Paket.", error);
        }
    }

    /// <summary>Prüft einzelne Lizenzangaben ohne automatische Rechtezuweisung.</summary>
    /// <param name="license">Der vollständige Lizenznachweis.</param>
    internal static void License(JsonElement license)
    {
        Fields(license, "id holder attribution license_url", "id holder attribution");
        Text(license, "id", 1, 128);
        Text(license, "holder", 1, 256);
        Text(license, "attribution", 1, 2048);
        OptionalUri(license, "license_url");
    }

    /// <summary>Prüft einzelne Quellen- und Bearbeitungsnachweise.</summary>
    /// <param name="provenance">Der vollständige Herkunftsnachweis.</param>
    internal static void Provenance(JsonElement provenance)
    {
        Fields(provenance, "kind source_url source_revision modification_note", "kind");
        var kind = Text(provenance, "kind", 1, 16);
        Require(kind is "original" or "adapted" or "verbatim", "Ungültige Herkunftsart.");
        OptionalUri(provenance, "source_url");
        OptionalText(provenance, "source_revision", 1, 256);
        OptionalText(provenance, "modification_note", 0, 2048);
        if (kind != "original")
        {
            Text(provenance, "source_url", 1, 2048);
            Text(provenance, "source_revision", 1, 256);
        }

        if (kind == "adapted")
        {
            Text(provenance, "modification_note", 1, 2048);
        }
    }

    private static Dictionary<string, byte[]> ReadFiles(ZipArchive zip)
    {
        Require(
            zip.Entries.Count is > 0 and <= 2001 && zip.Entries[0].FullName == "manifest.json",
            "manifest.json muss der erste ZIP-Eintrag sein; maximal 2001 Einträge.");
        var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        long total = 0;
        foreach (var entry in zip.Entries)
        {
            var path = entry.FullName;
            var kind = (entry.ExternalAttributes >> 16) & 0xf000;
            Require(
                (path == "manifest.json" || SafePath(path)) && !files.ContainsKey(path) &&
                kind is 0 or 0x8000,
                "Unsicherer, doppelter oder nicht regulärer ZIP-Eintrag.");
            total += entry.Length;
            Require(
                entry.Length <= MaxFileBytes && total <= 100L * 1024 * 1024 &&
                entry.Length <= Math.Max(entry.CompressedLength, 1) * 100,
                "ZIP überschreitet die Größen- oder Kompressionsgrenzen.");
            using var stream = entry.Open();
            using var content = new MemoryStream();
            var chunk = new byte[8192];
            int count;
            while ((count = stream.Read(chunk)) != 0)
            {
                Require(content.Length + count <= entry.Length, "Ungültige entpackte Dateigröße.");
                content.Write(chunk, 0, count);
            }

            Require(content.Length == entry.Length, "Unvollständige ZIP-Datei.");
            files.Add(path, content.ToArray());
        }

        return files;
    }

    private static void ValidateManifest(JsonElement manifest, Dictionary<string, byte[]> files)
    {
        ManifestMetadata(manifest, false);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Array(manifest, "files", 4, 2000))
        {
            Fields(file, "path sha256 size media_type", "path sha256 size");
            var path = Text(file, "path", 1, 256);
            var hash = Text(file, "sha256", 64, 64);
            Require(SafePath(path) && paths.Add(path) && files.ContainsKey(path), "Ungültige deklarierte Dateiliste.");
            Require(
                file.GetProperty("size").ValueKind == JsonValueKind.Number && file.GetProperty("size").TryGetInt64(out var size) && size == files[path].Length,
                "Dateigröße stimmt nicht: " + path);
            Require(HashPattern().IsMatch(hash) && Hash(files[path]) == hash, "Prüfsumme stimmt nicht: " + path);
            OptionalText(file, "media_type", 0, 128);
        }

        Require(paths.SetEquals(files.Keys.Where(path => path != "manifest.json")), "Dateiliste stimmt nicht mit ZIP überein.");
    }

    private static void ManifestMetadata(JsonElement manifest, bool origin)
    {
        Fields(
            manifest,
            "format_id schema_version package_id catalog_version source_revision title description language publisher created_at exporter_app_version license" + (origin ? string.Empty : " files"),
            "format_id schema_version package_id catalog_version source_revision title description language publisher created_at license" + (origin ? string.Empty : " files"));
        Require(Text(manifest, "format_id", 1, 128) == "org.learnpip.catalog.zip", "Unbekanntes Paketformat.");
        var version = Text(manifest, "schema_version", 1, 128);
        Require(version is "0.1.0" or "0.2.0", $"Schema-Version {version} ist nicht unterstützt. Unterstützt: 0.1.0, 0.2.0. Kein Teilimport.");
        Require(PackageIdPattern().IsMatch(Text(manifest, "package_id", 3, 128)), "Ungültige Paketkennung.");
        Text(manifest, "catalog_version", 1, 128);
        Text(manifest, "source_revision", 1, 256);
        Text(manifest, "title", 1, 256);
        Text(manifest, "description", 0, 2048);
        Text(manifest, "publisher", 1, 256);
        Language(manifest);
        OptionalText(manifest, "exporter_app_version", 1, 128);
        var date = Text(manifest, "created_at", 1, 128);
        Require(
            DatePattern().IsMatch(date) && DateTimeOffset.TryParse(
                date,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _),
            "Erstellungsdatum benötigt ein gültiges Datum mit Zeitzone.");
        License(manifest.GetProperty("license"));
    }

    private static void ValidateQuestion(
        JsonElement question,
        Dictionary<string,
        byte[]> files,
        HashSet<string> ids,
        HashSet<string> media,
        bool blocksFormat)
    {
        Fields(
            question,
            "id language prompt answers correct_answer_ids explanation topics difficulty age_band license provenance media" + (blocksFormat ? " subject topic question_version selection_mode prompt_blocks explanation_blocks source_note origin" : string.Empty),
            "id language prompt answers correct_answer_ids explanation topics difficulty license provenance media" + (blocksFormat ? " subject topic question_version selection_mode prompt_blocks explanation_blocks" : string.Empty));
        var id = Text(question, "id", 3, 128);
        Require(QuestionIdPattern().IsMatch(id) && ids.Add(id), "Ungültige oder doppelte Fragekennung.");
        Language(question);
        Text(question, "prompt", 1, blocksFormat ? 12000 : 10000);
        Text(question, "explanation", 0, blocksFormat ? 12000 : 20000);
        OptionalText(question, "age_band", 0, 80);
        var difficulty = Text(question, "difficulty", 1, 16);
        Require(difficulty is "unknown" or "easy" or "medium" or "hard", "Ungültiger Schwierigkeitsgrad.");
        License(question.GetProperty("license"));
        Provenance(question.GetProperty("provenance"));
        var topics = Array(question, "topics", 1, 10000);
        var topicNames = topics.Select(topic => String(topic, 1, 128)).ToArray();
        Require(topicNames.Distinct(StringComparer.Ordinal).Count() == topicNames.Length, "Doppelte Themen.");
        var answers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var answer in Array(question, "answers", 2, 20))
        {
            Fields(answer, blocksFormat ? "id text blocks" : "id text", blocksFormat ? "id text blocks" : "id text");
            var answerId = Text(answer, "id", 1, 64);
            Require(AnswerIdPattern().IsMatch(answerId) && answers.Add(answerId), "Ungültige Antwortkennung.");
            Text(answer, "text", 1, 10000);
        }

        var correct = Array(question, "correct_answer_ids", 1, 20).Select(item => String(item, 1, 64)).ToArray();
        Require(
            correct.Distinct(StringComparer.Ordinal).Count() == correct.Length && correct.All(answers.Contains),
            "Ungültige Lösung oder Antwortreferenz.");
        foreach (var asset in Array(question, "media", 0, blocksFormat ? 180 : 20))
        {
            Fields(asset, "path alt license provenance", "path alt license provenance");
            var path = Text(asset, "path", 1, 256);
            Require(
                path.StartsWith("media/", StringComparison.Ordinal) && SafePath(path) && files.ContainsKey(path),
                "Referenziertes Medium fehlt.");
            Text(asset, "alt", 1, 1024);
            License(asset.GetProperty("license"));
            Provenance(asset.GetProperty("provenance"));
            media.Add(path);
        }

        if (blocksFormat)
        {
            ValidateBlocksQuestion(question, correct.Length);
        }
    }

    private static void ValidateBlocksQuestion(JsonElement question, int correctCount)
    {
        Text(question, "subject", 1, 120);
        Text(question, "topic", 1, 120);
        Text(question, "question_version", 1, 128);
        OptionalText(question, "source_note", 0, 500);
        if (question.TryGetProperty("origin", out var origin))
        {
            ManifestMetadata(origin, true);
        }

        var mode = Text(question, "selection_mode", 1, 16);
        Require((mode == "single" && correctCount == 1) || (mode == "multiple" && correctCount >= 2), "Auswahlmodus und Lösungen passen nicht zusammen.");
        var declared = question.GetProperty("media").EnumerateArray().Select(item => item.GetProperty("path").GetString()!).ToHashSet(StringComparer.Ordinal);
        Require(declared.Count == question.GetProperty("media").GetArrayLength(), "Doppelte Medienbeschreibung.");
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        Blocks(question, "prompt_blocks", question.GetProperty("prompt").GetString()!, true, declared, referenced);
        Blocks(question, "explanation_blocks", question.GetProperty("explanation").GetString()!, false, declared, referenced);
        foreach (var answer in question.GetProperty("answers").EnumerateArray())
        {
            Blocks(answer, "blocks", answer.GetProperty("text").GetString()!, true, declared, referenced);
        }

        Require(declared.SetEquals(referenced), "Medienbeschreibung und Inhaltsblöcke stimmen nicht überein.");
    }

    private static void Blocks(JsonElement value, string name, string summary, bool required, HashSet<string> declared, HashSet<string> referenced)
    {
        var blocks = Array(value, name, required ? 1 : 0, 20);
        var text = new List<string>();
        foreach (var block in blocks)
        {
            var kind = Text(block, "kind", 1, 16);
            if (kind == "text")
            {
                Fields(block, "kind text", "kind text");
                text.Add(Text(block, "text", 1, 4000));
            }
            else
            {
                Fields(block, "kind path", "kind path");
                var path = Text(block, "path", 1, 256);
                Require(kind == "image" && declared.Contains(path), "Ungültiger Bildblock.");
                referenced.Add(path);
            }
        }

        Require(summary == (text.Count == 0 && required ? "[Bild]" : string.Join("\n", text)), "Textzusammenfassung und Inhaltsblöcke stimmen nicht überein.");
    }

    private static bool SafePath(string path) => RequiredFiles.Contains(path) ||
        (MediaPathPattern().IsMatch(path) && path.Split('/').All(part => part is not ("" or "." or "..")) &&
            MediaExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()));

    private static JsonElement Parse(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes.AsMemory(bytes.AsSpan().StartsWith(new byte[] { 0xef, 0xbb, 0xbf }) ? 3 : 0));
        Unique(document.RootElement);
        return document.RootElement.Clone();
    }

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                Require(names.Add(property.Name), "Doppelte JSON-Eigenschaft.");
                Unique(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                Unique(item);
            }
        }
    }

    private static void Fields(JsonElement value, string allowed, string required)
    {
        Require(value.ValueKind == JsonValueKind.Object, "JSON-Objekt erwartet.");
        var properties = value.EnumerateObject().Select(property => property.Name).ToArray();
        var names = properties.ToHashSet(StringComparer.Ordinal);
        Require(
            names.Count == properties.Length && names.IsSubsetOf(allowed.Split(' ')) && required.Split(' ').All(names.Contains),
            "Unbekannte oder fehlende JSON-Felder.");
    }

    private static JsonElement[] Array(JsonElement value, string name, int min, int max)
    {
        var array = value.GetProperty(name);
        Require(
            array.ValueKind == JsonValueKind.Array && array.GetArrayLength() >= min && array.GetArrayLength() <= max,
            "Ungültige Liste: " + name);
        return array.EnumerateArray().ToArray();
    }

    private static string Text(JsonElement value, string name, int min, int max) => String(value.GetProperty(name), min, max);

    private static string String(JsonElement value, int min, int max)
    {
        Require(value.ValueKind == JsonValueKind.String, "Text erwartet.");
        var text = value.GetString()!;
        Require(
            text.Length >= min && text.Length <= max && (min == 0 || !string.IsNullOrWhiteSpace(text)),
            "Textlänge außerhalb des erlaubten Bereichs.");
        return text;
    }

    private static void OptionalText(JsonElement value, string name, int min, int max)
    {
        if (value.TryGetProperty(name, out var text))
        {
            String(text, min, max);
        }
    }

    private static void OptionalUri(JsonElement value, string name)
    {
        if (value.TryGetProperty(name, out var text))
        {
            Require(Uri.TryCreate(String(text, 1, 2048), UriKind.Absolute, out _), "Ungültige Quellenadresse.");
        }
    }

    private static void Language(JsonElement value) => Require(
        LanguagePattern().IsMatch(Text(value, "language", 2, 35)),
        "Ungültiger Sprachcode.");

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private static string Fingerprint(JsonElement manifest, Dictionary<string, byte[]> files)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var property in manifest.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                if (property.Name != "files")
                {
                    writer.WritePropertyName(property.Name);
                    Canonical(writer, property.Value);
                }
            }

            writer.WritePropertyName("files");
            writer.WriteStartArray();
            foreach (var file in manifest.GetProperty("files").EnumerateArray().OrderBy(file => file.GetProperty("path").GetString(), StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("path", file.GetProperty("path").GetString());
                if (file.TryGetProperty("media_type", out var type))
                {
                    writer.WriteString("media_type", type.GetString());
                }

                var path = file.GetProperty("path").GetString()!;
                if (path == "questions.json")
                {
                    writer.WritePropertyName("content");
                    Canonical(writer, Parse(files[path]));
                }
                else
                {
                    writer.WriteString("sha256", Hash(files[path]));
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Hash(buffer.ToArray());
    }

    private static void Canonical(Utf8JsonWriter writer, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (var property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                writer.WritePropertyName(property.Name);
                Canonical(writer, property.Value);
            }

            writer.WriteEndObject();
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in value.EnumerateArray())
            {
                Canonical(writer, item);
            }

            writer.WriteEndArray();
        }
        else
        {
            value.WriteTo(writer);
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{2,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex PackageIdPattern();

    [GeneratedRegex("^[a-z0-9][a-z0-9._:-]{2,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex QuestionIdPattern();

    [GeneratedRegex("^[A-Za-z0-9_-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex AnswerIdPattern();

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashPattern();

    [GeneratedRegex("^media/[A-Za-z0-9._/-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex MediaPathPattern();

    [GeneratedRegex("^[a-z]{2,3}(-[A-Za-z0-9]{2,8})*$", RegexOptions.CultureInvariant)]
    private static partial Regex LanguagePattern();

    [GeneratedRegex("^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(\\.\\d+)?(Z|[+-]\\d{2}:\\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex DatePattern();
}
