// <copyright file="UserAiCredential.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell UserAiCredential.
/// </summary>
public sealed class UserAiCredential
{
    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt ciphertext.
    /// </summary>
    public string Ciphertext { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Änderungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
