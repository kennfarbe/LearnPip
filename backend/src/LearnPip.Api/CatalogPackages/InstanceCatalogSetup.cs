// <copyright file="InstanceCatalogSetup.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using LearnPip.Data;

namespace LearnPip.Api.CatalogPackages;

/// <summary>Optionale lokale Ersteinrichtung über den bereits geschützten Ops-Container.</summary>
public static class InstanceCatalogSetup
{
    /// <summary>Prüft einen bewusst ausgewählten ZIP-Stream und installiert ihn nur nach Bestätigung.</summary>
    /// <param name="input">Das Archiv über stdin; keine externen Downloads.</param>
    /// <param name="db">Der lokal berechtigte Ops-Datenbankkontext.</param>
    /// <param name="previewOnly">Ob lediglich die Vorschau ausgegeben wird.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Den Prozessstatus; Fehler sind kein erfolgreicher Import.</returns>
    public static async Task<int> Run(Stream input, LearnPipDbContext db, bool previewOnly, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var bytes = new byte[8192];
        int count;
        while ((count = await input.ReadAsync(bytes, ct)) != 0)
        {
            if (buffer.Length + count > CatalogPackageReader.MaxArchiveBytes)
            {
                throw new InvalidDataException("Paket überschreitet 25 MiB.");
            }

            await buffer.WriteAsync(bytes.AsMemory(0, count), ct);
        }

        var package = CatalogPackageReader.Read(buffer.ToArray());
        CatalogPackageImporter.PrepareImages(package);
        if (previewOnly)
        {
            Console.WriteLine(package.Manifest.GetRawText());
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Fragen = package.Questions.Count,
                ArchivBytes = package.Archive.Length,
                SpeicherbedarfBytes = package.Files.Values.Sum(file => (long)file.Length),
                Zielgruppen = package.Questions.Where(question => question.TryGetProperty("age_band", out _)).Select(question => question.GetProperty("age_band").GetString()).Distinct(StringComparer.Ordinal),
                Einzelnachweise = package.Questions.Select(question => new
                {
                    Lizenz = question.GetProperty("license"),
                    Herkunft = question.GetProperty("provenance"),
                    Medien = question.GetProperty("media"),
                }),
            }));
            foreach (var notice in package.Files.Where(file => file.Key is "LICENSES.md" or "NOTICE" or "ATTRIBUTION"))
            {
                Console.WriteLine(notice.Key);
                Console.WriteLine(Encoding.UTF8.GetString(notice.Value));
            }

            Console.WriteLine("Bereitstellung für Lernende dieser Instanz; keine persönlichen Fragen oder Lernstände werden verändert. Vertrauliche Kontodaten dürfen nicht im Paket stehen.");
            return 0;
        }

        var result = await InstanceCatalogEndpoints.Store(package, null, db, new ClaimsPrincipal(), ct);
        var status = (result as IStatusCodeHttpResult)?.StatusCode ?? 500;
        Console.WriteLine(status is >= 200 and < 300 ? "Optionales Instanzpaket eingerichtet." : "Paket nicht eingerichtet; Konflikt oder Speichergrenze. Administration verwenden.");
        return status is >= 200 and < 300 ? 0 : 1;
    }
}
