// <copyright file="PrivateMedia.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace LearnPip.Api.Media;

// A storage boundary so a private object store can replace PostgreSQL without changing the API.
public interface IPrivateMediaStore
{
    void Add(MediaAsset asset, byte[] bytes);
    Task<byte[]?> ReadAsync(Guid mediaId, CancellationToken cancellationToken);
    void Remove(MediaAsset asset);
}

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

public static class PrivateImageProcessor
{
    public const long MaxUploadBytes = 5 * 1024 * 1024;
    private const long MaxPixels = 16_000_000;

    public static (byte[] Bytes, string MediaType)? Sanitize(byte[] bytes, string declaredType)
    {
        var type = declaredType.ToLowerInvariant();
        if (type is not ("image/jpeg" or "image/png")) return null;
        using var source = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(source);
        if (codec == null || codec.Info.Width < 1 || codec.Info.Height < 1 ||
            codec.Info.Width > 4096 || codec.Info.Height > 4096 ||
            (long)codec.Info.Width * codec.Info.Height > MaxPixels || codec.FrameCount > 1)
            return null;
        var format = type == "image/jpeg" ? SKEncodedImageFormat.Jpeg : SKEncodedImageFormat.Png;
        if (codec.EncodedFormat != format) return null;
        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap == null) return null;
        using var encoded = bitmap.Encode(format, 85);
        if (encoded == null || encoded.Size > MaxUploadBytes) return null;
        // Re-encoding pixels drops EXIF, GPS, text chunks and embedded profiles.
        return (encoded.ToArray(), type);
    }
}
