// <copyright file="CatalogPackageUpdates.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Text.Json;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Schützt lokale Bearbeitungen und historische Lernfassungen beim Paketwechsel.</summary>
internal static class CatalogPackageUpdates
{
    /// <summary>Prüft den unveränderten importierten Basisstand statt persönlicher Fassungen.</summary>
    /// <param name="question">Die Frage mit geladenen Fassungen und Entwurf.</param>
    /// <param name="versionsJson">Die zuletzt übernommenen Basisfassungen.</param>
    /// <param name="sourceId">Die externe Fragekennung.</param>
    /// <returns>Ob keine persönliche Änderung oder Löschung vorliegt.</returns>
    internal static bool Unedited(Question question, string versionsJson, string sourceId)
    {
        var versions = JsonSerializer.Deserialize<Dictionary<string, Guid>>(versionsJson)!;
        var latest = question.Versions.MaxBy(version => version.VersionNumber);
        return question.DeletedAtUtc == null && question.Draft == null && latest != null &&
            (versions.TryGetValue(sourceId, out var basis) ? latest.Id == basis : question.Versions.Count == 1 && latest.VersionNumber == 1);
    }

    /// <summary>Lädt nur eigene unpersönlich bearbeitete Quellfragen eines bestehenden Pakets.</summary>
    /// <param name="import">Der bestehende private Import.</param>
    /// <param name="db">Der eigene Datenbankkontext.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Die ursprünglichen Fragen mit unveränderlichen historischen Fassungen.</returns>
    internal static async Task<Dictionary<string, Question>> Candidates(CatalogPackageImport import, LearnPipDbContext db, CancellationToken ct)
    {
        var ids = JsonSerializer.Deserialize<Dictionary<string, Guid>>(import.QuestionIdsJson)!;
        var values = ids.Values.ToArray();
        var questions = await db.Questions.Where(question => values.Contains(question.Id) && question.OwnerAccountId == import.OwnerAccountId)
            .Include(question => question.Draft).Include(question => question.Versions).ToDictionaryAsync(question => question.Id, ct);
        if (questions.Count != ids.Count || ids.Any(pair => !Unedited(questions[pair.Value], import.QuestionVersionIdsJson, pair.Key)))
        {
            throw new InvalidDataException("Das Paket enthält persönlich bearbeitete oder gelöschte Fragen. Update gesperrt; eigene Änderungen und Lernstände bleiben erhalten. Bitte Änderungen separat sichern und den Konflikt ausdrücklich lösen.");
        }

        return ids.ToDictionary(pair => pair.Key, pair => questions[pair.Value], StringComparer.Ordinal);
    }

    /// <summary>Speichert die tatsächlich übernommenen Versionen als neue Bearbeitungsbasis.</summary>
    /// <param name="db">Der Kontext einschließlich noch nicht gespeicherter Fassungen.</param>
    /// <param name="ids">Die vollständige Quellzuordnung.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Die JSON-Zuordnung jeder Quellfrage zur übernommenen Version.</returns>
    internal static async Task<string> Versions(LearnPipDbContext db, IReadOnlyDictionary<string, Guid> ids, CancellationToken ct)
    {
        var values = ids.Values.ToArray();
        var existing = await db.QuestionVersions.AsNoTracking().Where(version => values.Contains(version.QuestionId)).ToListAsync(ct);
        var versions = existing.Concat(db.QuestionVersions.Local).DistinctBy(version => version.Id)
            .GroupBy(version => version.QuestionId).ToDictionary(group => group.Key, group => group.MaxBy(version => version.VersionNumber)!.Id);
        return JsonSerializer.Serialize(ids.ToDictionary(pair => pair.Key, pair => versions[pair.Value], StringComparer.Ordinal));
    }
}
