// <copyright file="MediaAsset.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell MediaAsset.
/// </summary>
public sealed class MediaAsset
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Eigentümerkontos.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt question version id.
    /// </summary>
    public Guid? QuestionVersionId { get; set; }

    /// <summary>
    /// Holt oder setzt storage key.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt media type.
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt alt text.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt byte length.
    /// </summary>
    public long ByteLength { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt Löschzeitpunkt in UTC, sofern vorhanden.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt owner.
    /// </summary>
    public Account Owner { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt question version.
    /// </summary>
    public QuestionVersion? QuestionVersion { get; set; }
}
