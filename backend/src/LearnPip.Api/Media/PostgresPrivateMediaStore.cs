// <copyright file="PostgresPrivateMediaStore.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace LearnPip.Api.Media;

public sealed class PostgresPrivateMediaStore(LearnPipDbContext db) : IPrivateMediaStore
{
    public void Add(MediaAsset asset, byte[] bytes)
    {
        db.MediaAssets.Add(asset);
        db.MediaBlobs.Add(new MediaBlob { MediaAssetId = asset.Id, Data = bytes });
    }

    public Task<byte[]?> ReadAsync(Guid mediaId, CancellationToken cancellationToken) =>
        db.MediaBlobs.AsNoTracking().Where(item => item.MediaAssetId == mediaId)
            .Select(item => item.Data).SingleOrDefaultAsync(cancellationToken);

    public void Remove(MediaAsset asset) => db.MediaAssets.Remove(asset);
}
