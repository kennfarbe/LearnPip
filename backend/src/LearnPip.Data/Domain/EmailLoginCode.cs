// <copyright file="EmailLoginCode.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell EmailLoginCode.
/// </summary>
public sealed class EmailLoginCode
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt initiating session id.
    /// </summary>
    public Guid? InitiatingSessionId { get; set; }

    /// <summary>
    /// Holt oder setzt code hash.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt expires at utc.
    /// </summary>
    public DateTimeOffset ExpiresAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt consumed at utc.
    /// </summary>
    public DateTimeOffset? ConsumedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt failed attempts.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>
    /// Holt oder setzt zugeordnetes Konto.
    /// </summary>
    public Account? Account { get; set; }

    /// <summary>
    /// Holt oder setzt initiating session.
    /// </summary>
    public AccountSession? InitiatingSession { get; set; }
}
