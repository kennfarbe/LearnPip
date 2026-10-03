// <copyright file="PrivateMediaCatalog.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data.Services;

/// <summary>
/// Ermöglicht den berechtigten Zugriff auf private Medien eines Kontos.
/// </summary>
/// <param name="dbContext">Datenbankkontext für private Medien.</param>
public sealed class PrivateMediaCatalog(LearnPipDbContext dbContext)
{
    /// <summary>Lädt die nicht gelöschten Medien eines Kontos in absteigender Erstellungsreihenfolge.</summary>
    /// <param name="accountId">Kennung des Eigentümerkontos.</param>
    /// <param name="cancellationToken">Token zum Abbrechen.</param>
    /// <returns>Die eigenen Medien des Kontos.</returns>
    public Task<List<MediaAsset>> ListOwnedByAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.MediaAssets
            .AsNoTracking()
            .Where(asset => asset.OwnerAccountId == accountId && asset.DeletedAtUtc == null)
            .OrderByDescending(asset => asset.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
