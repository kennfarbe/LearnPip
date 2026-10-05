// <copyright file="CatalogPackageImporter.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Data;
using LearnPip.Data.Domain;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Erstellt private Lernfragen, ohne Originalpakete oder bestehende Fragen zu ändern.</summary>
public static class CatalogPackageImporter
{
    /// <summary>Prüft vor der Transaktion die Darstellbarkeit aller enthaltenen Medien.</summary>
    /// <param name="package">Das vollständig validierte Originalpaket.</param>
    /// <returns>Bereinigte Anzeigebilder; Originalbytes bleiben im Archiv erhalten.</returns>
    /// <exception cref="InvalidDataException">Ein Inhalt kann nicht vollständig übernommen werden.</exception>
    public static IReadOnlyDictionary<string, byte[]> PrepareImages(CatalogPackage package)
    {
        if (ContainsNullCharacter(package.Manifest))
        {
            throw new InvalidDataException("Nullzeichen in Paketmetadaten sind nicht speicherbar. Kein Teilimport.");
        }

        var images = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (path, bytes) in package.Files.Where(file => file.Key.StartsWith("media/", StringComparison.Ordinal)))
        {
            var type = MediaType(path);
            var image = bytes.Length <= PrivateImageProcessor.MaxUploadBytes ? PrivateImageProcessor.Sanitize(bytes, type) : null;
            if (image == null)
            {
                throw new InvalidDataException("Für den privaten Import sind derzeit nur gültige JPEG-/PNG-Bilder bis 5 MiB und 4096 × 4096 Pixel unterstützt. Kein Teilimport.");
            }

            images.Add(path, image.Value.Bytes);
        }

        foreach (var question in package.Questions)
        {
            if (question.GetProperty("media").EnumerateArray().Any(asset => asset.GetProperty("alt").GetString()!.Length > 300))
            {
                throw new InvalidDataException("Bildbeschreibungen dürfen im privaten Import derzeit maximal 300 Zeichen umfassen. Kein Teilimport.");
            }

            if (ContainsNullCharacter(question) ||
                question.GetProperty("prompt").GetString()!.Length > 4000 ||
                question.GetProperty("explanation").GetString()!.Length > 4000 ||
                question.GetProperty("answers").EnumerateArray().Any(answer => answer.GetProperty("text").GetString()!.Length > 4000) ||
                question.GetProperty("license").GetProperty("id").GetString()!.Length > 120)
            {
                throw new InvalidDataException("Ein Inhalt überschreitet die Grenzen des Fragenmodells. Originalpaket bleibt unverändert; kein Teilimport.");
            }
        }

        return images;
    }

    /// <summary>Fügt sämtliche neuen privaten Fragen einem bereits gesperrten Importkontext hinzu.</summary>
    /// <param name="db">Der Datenbankkontext in einer gemeinsamen Transaktion.</param>
    /// <param name="package">Das vollständig geprüfte Paket.</param>
    /// <param name="images">Die vollständig geprüften Anzeigebilder.</param>
    /// <param name="owner">Das lokale Eigentümerkonto.</param>
    /// <param name="catalog">Der private Zielkatalog.</param>
    /// <returns>Die Zuordnung stabiler externer IDs zu lokalen Fragen.</returns>
    public static IReadOnlyDictionary<string, Guid> AddQuestions(
        LearnPipDbContext db,
        CatalogPackage package,
        IReadOnlyDictionary<string,
        byte[]> images,
        Guid owner,
        PrivateCatalog catalog)
    {
        var mappings = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var original in package.Questions)
        {
            var question = new Question { OwnerAccountId = owner, PrivateCatalogId = catalog.Id };
            var topic = original.GetProperty("topics")[0].GetString()!;
            question.LearningContent = new LearningContent { Id = question.Id, OwnerAccountId = owner, Title = Short(topic) };
            var version = new QuestionVersion
            {
                Question = question,
                CreatedByAccountId = owner,
                VersionNumber = 1,
                Visibility = "private",
                Prompt = original.GetProperty("prompt").GetString()!,
                Explanation = original.GetProperty("explanation").GetString(),
                SelectionMode = original.GetProperty("correct_answer_ids").GetArrayLength() == 1 ? "single" : "multiple",
                Language = original.GetProperty("language").GetString()!,
                Subject = Short(topic),
                Topic = Short(topic),
                Source = package.Manifest.GetProperty("package_id").GetString()!,
                License = original.GetProperty("license").GetProperty("id").GetString()!,
                AuthorAttribution = Short(original.GetProperty("license").GetProperty("attribution").GetString()!),
            };
            db.Questions.Add(question);
            db.QuestionVersions.Add(version);
            db.QuestionContentBlocks.Add(new QuestionContentBlock
            {
                QuestionVersionId = version.Id, Section = "prompt", Kind = "text", Text = version.Prompt,
            });
            db.QuestionContentBlocks.Add(new QuestionContentBlock
            {
                QuestionVersionId = version.Id, Section = "explanation", Kind = "text", Text = version.Explanation,
            });
            var correct = original.GetProperty("correct_answer_ids").EnumerateArray().Select(answer => answer.GetString()).ToHashSet(StringComparer.Ordinal);
            var index = 0;
            foreach (var answer in original.GetProperty("answers").EnumerateArray())
            {
                var option = new AnswerOption
                {
                    QuestionVersionId = version.Id, SortOrder = index++,
                    IsCorrect = correct.Contains(answer.GetProperty("id").GetString()),
                    Text = answer.GetProperty("text").GetString()!,
                };
                db.AnswerOptions.Add(option);
                db.QuestionContentBlocks.Add(new QuestionContentBlock
                {
                    AnswerOptionId = option.Id, Section = "answer", Kind = "text", Text = option.Text,
                });
            }

            index = 1;
            foreach (var asset in original.GetProperty("media").EnumerateArray())
            {
                var path = asset.GetProperty("path").GetString()!;
                var image = new MediaAsset
                {
                    OwnerAccountId = owner, QuestionVersionId = version.Id,
                    StorageKey = $"postgres/{Guid.NewGuid():N}", MediaType = MediaType(path),
                    AltText = asset.GetProperty("alt").GetString()!, ByteLength = images[path].Length,
                };
                db.MediaAssets.Add(image);
                db.MediaBlobs.Add(new MediaBlob { MediaAssetId = image.Id, Data = images[path] });
                db.QuestionContentBlocks.Add(new QuestionContentBlock
                {
                    QuestionVersionId = version.Id, Section = "prompt", SortOrder = index++,
                    Kind = "image", MediaAssetId = image.Id,
                });
            }

            mappings.Add(original.GetProperty("id").GetString()!, question.Id);
        }

        return mappings;
    }

    private static bool ContainsNullCharacter(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!.Contains('\0', StringComparison.Ordinal),
        JsonValueKind.Object => value.EnumerateObject().Any(property => ContainsNullCharacter(property.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(ContainsNullCharacter),
        _ => false,
    };

    private static string Short(string value) => string.Concat(value.EnumerateRunes().Take(120));

    private static string MediaType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "unsupported",
    };
}
