// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class MediaBlob
{
    public Guid MediaAssetId { get; set; }

    public byte[] Data { get; set; } = [];

    public MediaAsset MediaAsset { get; set; } = null!;
}
