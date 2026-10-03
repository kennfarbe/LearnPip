// <copyright file="IPrivateMediaStore.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace LearnPip.Api.Media;

// A storage boundary so a private object store can replace PostgreSQL without changing the API.

/// <summary>
/// Schnittstelle für die Speicherung und den Abruf privater Medien.
/// </summary>
public interface IPrivateMediaStore
{
    /// <summary>
    /// Fügt ein Medium mit seinen privaten Bilddaten zur Datenbank hinzu.
    /// </summary>
    /// <param name="asset">Der Datensatz des privaten Mediums.</param>
    /// <param name="bytes">Die privaten Bilddaten.</param>
    void Add(MediaAsset asset, byte[] bytes);

    /// <summary>
    /// Lädt die privaten Bilddaten eines Mediums.
    /// </summary>
    /// <param name="mediaId">Die Kennung des privaten Mediums.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    Task<byte[]?> ReadAsync(Guid mediaId, CancellationToken cancellationToken);

    /// <summary>
    /// Markiert ein privates Medium zur Löschung.
    /// </summary>
    /// <param name="asset">Der Datensatz des privaten Mediums.</param>
    void Remove(MediaAsset asset);
}
