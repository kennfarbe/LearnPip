// <copyright file="CatalogPackageComparison.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using System.Text.Json.Nodes;
using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Vergleicht stabile Quell-IDs auch zwischen überlappenden Exportauswahlen.</summary>
internal static class CatalogPackageComparison
{
    /// <summary>Verweigert geänderte Quellfragen und verwendet unveränderte Fragen erneut.</summary>
    /// <param name="package">Das vollständig validierte neue Paket.</param>
    /// <param name="db">Der Datenbankkontext des eigenen Kontos.</param>
    /// <param name="owner">Das eigene Konto.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Identische bestehende Fragen und konfliktbehaftete Quell-IDs.</returns>
    internal static async Task<(Dictionary<string, Guid> Identical, List<string> Conflicts)> Compare(CatalogPackage package, LearnPipDbContext db, Guid owner, CancellationToken ct)
    {
        var imports = await db.CatalogPackageImports.AsNoTracking().Where(item => item.OwnerAccountId == owner)
            .Select(item => new { item.Id, item.QuestionIdsJson }).ToListAsync(ct);
        var sourceIds = package.Questions.Select(question => question.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);
        var known = new Dictionary<string, (Guid ImportId, Guid QuestionId)>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        foreach (var import in imports)
        {
            foreach (var (sourceId, questionId) in JsonSerializer.Deserialize<Dictionary<string, Guid>>(import.QuestionIdsJson)!.Where(pair => sourceIds.Contains(pair.Key)))
            {
                if (known.TryGetValue(sourceId, out var previous) && previous.QuestionId != questionId)
                {
                    ambiguous.Add(sourceId);
                }
                else
                {
                    known.TryAdd(sourceId, (import.Id, questionId));
                }
            }
        }

        var originals = new Dictionary<Guid, CatalogPackage>();
        var identical = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var conflicts = new List<string>();
        foreach (var question in package.Questions)
        {
            var sourceId = question.GetProperty("id").GetString()!;
            if (!known.TryGetValue(sourceId, out var origin))
            {
                const string prefix = "learnpip-question:";
                if (sourceId.StartsWith(prefix, StringComparison.Ordinal) && Guid.TryParseExact(sourceId[prefix.Length..], "N", out var localId) &&
                    await db.Questions.AnyAsync(item => item.Id == localId && item.OwnerAccountId == owner, ct))
                {
                    // An imported archive must never create a second copy of an existing native question.
                    conflicts.Add(sourceId);
                }

                continue;
            }

            var unedited = await db.Questions.AnyAsync(
                item => item.Id == origin.QuestionId && item.OwnerAccountId == owner && item.DeletedAtUtc == null &&
                item.Draft == null && item.Versions.Count == 1 && item.Versions.Any(version => version.VersionNumber == 1),
                ct);
            if (ambiguous.Contains(sourceId) || !unedited)
            {
                conflicts.Add(sourceId);
                continue;
            }

            if (!originals.TryGetValue(origin.ImportId, out var original))
            {
                var bytes = await db.CatalogPackageImports.Where(item => item.Id == origin.ImportId && item.OwnerAccountId == owner).Select(item => item.Archive).SingleAsync(ct);
                original = CatalogPackageReader.Read(bytes);
                originals.Add(origin.ImportId, original);
            }

            var previousQuestion = original.Questions.Single(item => item.GetProperty("id").GetString() == sourceId);
            var same = JsonNode.DeepEquals(Normalize(question, package.Files), Normalize(previousQuestion, original.Files));
            if (same)
            {
                identical.Add(sourceId, origin.QuestionId);
            }
            else
            {
                conflicts.Add(sourceId);
            }
        }

        return (identical, conflicts);
    }

    private static JsonObject Normalize(JsonElement question, IReadOnlyDictionary<string, byte[]> files)
    {
        var node = JsonNode.Parse(question.GetRawText())!.AsObject();
        var paths = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var asset in question.GetProperty("media").EnumerateArray())
        {
            var path = asset.GetProperty("path").GetString()!;
            var metadata = new
            {
                Alt = asset.GetProperty("alt").GetString(),
                License = asset.GetProperty("license").EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new[] { property.Name, property.Value.GetString() }).ToArray(),
                Provenance = asset.GetProperty("provenance").EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new[] { property.Name, property.Value.GetString() }).ToArray(),
            };
            var bytesHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(files[path]));
            var metadataHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(metadata)));
            paths.Add(path, "media/" + bytesHash + "-" + metadataHash);
        }

        foreach (var asset in node["media"]!.AsArray())
        {
            asset!["path"] = paths[asset["path"]!.GetValue<string>()];
        }

        if (node.ContainsKey("prompt_blocks"))
        {
            foreach (var block in node["prompt_blocks"]!.AsArray().Concat(node["explanation_blocks"]!.AsArray())
                .Concat(node["answers"]!.AsArray().SelectMany(answer => answer!["blocks"]!.AsArray())).Where(block => block!["kind"]!.GetValue<string>() == "image"))
            {
                block!["path"] = paths[block["path"]!.GetValue<string>()];
            }

            node["media"] = new JsonArray(node["media"]!.AsArray().OrderBy(asset => asset!["path"]!.GetValue<string>(), StringComparer.Ordinal).Select(asset => asset!.DeepClone()).ToArray());
        }

        return node;
    }
}
