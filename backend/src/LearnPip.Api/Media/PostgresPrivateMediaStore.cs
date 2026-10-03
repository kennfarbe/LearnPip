// <copyright file="PostgresPrivateMediaStore.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace LearnPip.Api.Media;

/// <summary>
/// Speichert private Medieninhalte in der PostgreSQL-Datenbank.
/// </summary>
/// <param name="db">Der Datenbankkontext.</param>
public sealed class PostgresPrivateMediaStore(LearnPipDbContext db) : IPrivateMediaStore
{
    /// <summary>
    /// Fügt ein Medium mit seinen privaten Bilddaten zur Datenbank hinzu.
    /// </summary>
    /// <param name="asset">Der Datensatz des privaten Mediums.</param>
    /// <param name="bytes">Die privaten Bilddaten.</param>
    public void Add(
        MediaAsset asset,
        byte[] bytes)
    {
        db.MediaAssets.Add(asset);
        db.MediaBlobs.Add(new MediaBlob { MediaAssetId = asset.Id, Data = bytes });
    }

    /// <summary>
    /// Lädt die privaten Bilddaten eines Mediums.
    /// </summary>
    /// <param name="mediaId">Die Kennung des privaten Mediums.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<byte[]?> ReadAsync(Guid mediaId, CancellationToken cancellationToken) =>
        db.MediaBlobs.AsNoTracking().Where(item => item.MediaAssetId == mediaId)
            .Select(item => item.Data).SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Markiert ein privates Medium zur Löschung.
    /// </summary>
    /// <param name="asset">Der Datensatz des privaten Mediums.</param>
    public void Remove(MediaAsset asset) => db.MediaAssets.Remove(asset);
}
