// <copyright file="CatalogPackageImporter.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using LearnPip.Api.Media;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

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
            var paths = question.GetProperty("media").EnumerateArray().Select(asset => asset.GetProperty("path").GetString()).ToArray();
            if (paths.Length != paths.Distinct(StringComparer.Ordinal).Count())
            {
                throw new InvalidDataException("Doppelte Medienbeschreibungen können nicht eindeutig übernommen werden. Kein Teilimport.");
            }

            if (question.GetProperty("media").EnumerateArray().Any(asset => asset.GetProperty("alt").GetString()!.Length > 300))
            {
                throw new InvalidDataException("Bildbeschreibungen dürfen im privaten Import derzeit maximal 300 Zeichen umfassen. Kein Teilimport.");
            }

            if (ContainsNullCharacter(question) ||
                question.GetProperty("prompt").GetString()!.Length > (question.TryGetProperty("prompt_blocks", out _) ? 12000 : 4000) ||
                question.GetProperty("explanation").GetString()!.Length > (question.TryGetProperty("prompt_blocks", out _) ? 12000 : 4000) ||
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
    /// <param name="identical">Bereits vorhandene unveränderte Quellfragen.</param>
    /// <param name="updates">Ausdrücklich zu aktualisierende unpersönlich bearbeitete Fragen.</param>
    /// <returns>Die Zuordnung stabiler externer IDs zu lokalen Fragen.</returns>
    public static IReadOnlyDictionary<string, Guid> AddQuestions(
        LearnPipDbContext db,
        CatalogPackage package,
        IReadOnlyDictionary<string,
        byte[]> images,
        Guid owner,
        PrivateCatalog catalog,
        IReadOnlyDictionary<string,
        Guid>? identical = null,
        IReadOnlyDictionary<string, Question>? updates = null)
    {
        var reusedIds = (identical?.Values ?? []).Concat(updates?.Values.Select(item => item.Id) ?? []).Distinct().ToArray();
        var reused = db.Questions.Include(item => item.CatalogMemberships)
            .Where(item => item.OwnerAccountId == owner && reusedIds.Contains(item.Id)).ToDictionary(item => item.Id);
        var mappings = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var original in package.Questions)
        {
            var sourceId = original.GetProperty("id").GetString()!;
            if (identical != null && identical.TryGetValue(sourceId, out var known))
            {
                AddMembership(reused[known], catalog.Id);
                mappings.Add(sourceId, known);
                continue;
            }

            var updating = updates != null && updates.TryGetValue(sourceId, out _);
            var question = updating ? updates![sourceId] : new Question { OwnerAccountId = owner, PrivateCatalogId = catalog.Id };
            AddMembership(question, catalog.Id);
            var topic = original.GetProperty("topics")[0].GetString()!;
            var blocksFormat = original.TryGetProperty("prompt_blocks", out _);
            if (!updating)
            {
                question.LearningContent = new LearningContent { Id = question.Id, OwnerAccountId = owner, Title = Short(topic) };
            }

            question.UpdatedAtUtc = DateTimeOffset.UtcNow;
            var selectionMode = original.GetProperty("correct_answer_ids").GetArrayLength() == 1 ? "single" : "multiple";
            var version = new QuestionVersion
            {
                Question = question,
                CreatedByAccountId = owner,
                VersionNumber = updating ? question.Versions.Max(item => item.VersionNumber) + 1 : 1,
                Visibility = "private",
                Prompt = original.GetProperty("prompt").GetString()!,
                Explanation = original.GetProperty("explanation").GetString(),
                SelectionMode = blocksFormat ? original.GetProperty("selection_mode").GetString()! : selectionMode,
                Language = original.GetProperty("language").GetString()!,
                Subject = blocksFormat ? original.GetProperty("subject").GetString()! : Short(topic),
                Topic = blocksFormat ? original.GetProperty("topic").GetString()! : Short(topic),
                Source = original.TryGetProperty("source_note", out var sourceNote) ? sourceNote.GetString()! : package.Manifest.GetProperty("package_id").GetString()!,
                License = original.GetProperty("license").GetProperty("id").GetString()!,
                AuthorAttribution = Short(original.GetProperty("license").GetProperty("attribution").GetString()!),
            };
            if (!updating)
            {
                db.Questions.Add(question);
            }

            db.QuestionVersions.Add(version);
            var media = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var asset in original.GetProperty("media").EnumerateArray())
            {
                var path = asset.GetProperty("path").GetString()!;
                var image = new MediaAsset
                {
                    OwnerAccountId = owner,
                    QuestionVersionId = version.Id,
                    StorageKey = $"postgres/{Guid.NewGuid():N}",
                    MediaType = MediaType(path),
                    AltText = asset.GetProperty("alt").GetString()!,
                    ByteLength = images[path].Length,
                };
                db.MediaAssets.Add(image);
                db.MediaBlobs.Add(new MediaBlob { MediaAssetId = image.Id, Data = images[path] });
                media.Add(path, image.Id);
            }

            if (blocksFormat)
            {
                AddBlocks(db, version.Id, null, "prompt", original.GetProperty("prompt_blocks"), media);
                AddBlocks(db, version.Id, null, "explanation", original.GetProperty("explanation_blocks"), media);
            }
            else
            {
                db.QuestionContentBlocks.Add(new QuestionContentBlock { QuestionVersionId = version.Id, Section = "prompt", Kind = "text", Text = version.Prompt });
                db.QuestionContentBlocks.Add(new QuestionContentBlock { QuestionVersionId = version.Id, Section = "explanation", Kind = "text", Text = version.Explanation });
                var order = 1;
                foreach (var image in media.Values)
                {
                    db.QuestionContentBlocks.Add(new QuestionContentBlock { QuestionVersionId = version.Id, Section = "prompt", SortOrder = order++, Kind = "image", MediaAssetId = image });
                }
            }

            var correct = original.GetProperty("correct_answer_ids").EnumerateArray().Select(answer => answer.GetString()).ToHashSet(StringComparer.Ordinal);
            var index = 0;
            foreach (var answer in original.GetProperty("answers").EnumerateArray())
            {
                var option = new AnswerOption
                {
                    QuestionVersionId = version.Id,
                    SortOrder = index++,
                    IsCorrect = correct.Contains(answer.GetProperty("id").GetString()),
                    Text = answer.GetProperty("text").GetString()!,
                };
                db.AnswerOptions.Add(option);
                if (blocksFormat)
                {
                    AddBlocks(db, null, option.Id, "answer", answer.GetProperty("blocks"), media);
                }
                else
                {
                    db.QuestionContentBlocks.Add(new QuestionContentBlock { AnswerOptionId = option.Id, Section = "answer", Kind = "text", Text = option.Text });
                }
            }

            mappings.Add(original.GetProperty("id").GetString()!, question.Id);
        }

        return mappings;
    }

    private static void AddBlocks(LearnPipDbContext db, Guid? versionId, Guid? answerId, string section, JsonElement blocks, Dictionary<string, Guid> media)
    {
        var index = 0;
        foreach (var block in blocks.EnumerateArray())
        {
            var kind = block.GetProperty("kind").GetString()!;
            db.QuestionContentBlocks.Add(new QuestionContentBlock
            {
                QuestionVersionId = versionId,
                AnswerOptionId = answerId,
                Section = section,
                SortOrder = index++,
                Kind = kind,
                Text = kind == "text" ? block.GetProperty("text").GetString() : null,
                MediaAssetId = kind == "image" ? media[block.GetProperty("path").GetString()!] : null,
            });
        }
    }

    private static bool ContainsNullCharacter(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!.Contains('\0', StringComparison.Ordinal),
        JsonValueKind.Object => value.EnumerateObject().Any(property => ContainsNullCharacter(property.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(ContainsNullCharacter),
        _ => false,
    };

    private static string Short(string value) => string.Concat(value.EnumerateRunes().Take(120));

    private static void AddMembership(Question question, Guid catalogId)
    {
        if (question.CatalogMemberships.Any(item => item.CatalogId == catalogId))
        {
            return;
        }

        if (question.CatalogMemberships.Count >= 100)
        {
            throw new InvalidDataException("Eine Frage kann höchstens 100 Katalogen zugeordnet werden.");
        }

        question.CatalogMemberships.Add(new QuestionCatalogMembership { CatalogId = catalogId, Question = question });
    }

    private static string MediaType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "unsupported",
    };
}
