// <copyright file="IPrivateMediaStore.cs" company="LearnPip contributors">
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
