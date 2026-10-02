// <copyright file="MediaBlob.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell MediaBlob.
/// </summary>
public sealed class MediaBlob
{
    /// <summary>
    /// Holt oder setzt media asset id.
    /// </summary>
    public Guid MediaAssetId { get; set; }

    /// <summary>
    /// Holt oder setzt data.
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// Holt oder setzt media asset.
    /// </summary>
    public MediaAsset MediaAsset { get; set; } = null!;
}
