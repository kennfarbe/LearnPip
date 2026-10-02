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
    /// Ruft media asset id ab oder legt den Wert fest.
    /// </summary>
    public Guid MediaAssetId { get; set; }

    /// <summary>
    /// Ruft data ab oder legt den Wert fest.
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// Ruft media asset ab oder legt den Wert fest.
    /// </summary>
    public MediaAsset MediaAsset { get; set; } = null!;
}
