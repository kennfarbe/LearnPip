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
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft Kennung des Eigentümerkontos ab oder legt den Wert fest.
    /// </summary>
    public Guid OwnerAccountId { get; set; }

    /// <summary>
    /// Ruft question version id ab oder legt den Wert fest.
    /// </summary>
    public Guid? QuestionVersionId { get; set; }

    /// <summary>
    /// Ruft storage key ab oder legt den Wert fest.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>
    /// Ruft media type ab oder legt den Wert fest.
    /// </summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// Ruft alt text ab oder legt den Wert fest.
    /// </summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>
    /// Ruft byte length ab oder legt den Wert fest.
    /// </summary>
    public long ByteLength { get; set; }

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft Löschzeitpunkt in UTC, sofern vorhanden ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Ruft owner ab oder legt den Wert fest.
    /// </summary>
    public Account Owner { get; set; } = null!;

    /// <summary>
    /// Ruft question version ab oder legt den Wert fest.
    /// </summary>
    public QuestionVersion? QuestionVersion { get; set; }
}
