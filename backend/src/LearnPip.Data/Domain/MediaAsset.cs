// <copyright file="MediaAsset.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerAccountId { get; set; }

    public Guid? QuestionVersionId { get; set; }

    public string StorageKey { get; set; } = string.Empty;

    public string MediaType { get; set; } = string.Empty;

    public string AltText { get; set; } = string.Empty;

    public long ByteLength { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Account Owner { get; set; } = null!;

    public QuestionVersion? QuestionVersion { get; set; }
}
