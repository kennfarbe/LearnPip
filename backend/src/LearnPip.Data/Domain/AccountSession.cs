// <copyright file="AccountSession.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AccountSession.
/// </summary>
public sealed class AccountSession
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt token hash.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt expires at utc.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt revoked at utc.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt zugeordnetes Konto.
    /// </summary>
    public Account Account { get; set; } = null!;
}
