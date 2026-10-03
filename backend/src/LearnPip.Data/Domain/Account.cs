// <copyright file="Account.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell Account.
/// </summary>
public sealed class Account
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Holt oder setzt age band.
    /// </summary>
    public string AgeBand { get; set; } = "unknown";

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt Änderungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt Löschzeitpunkt in UTC, sofern vorhanden.
    /// </summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt last activity at utc.
    /// </summary>
    public DateTimeOffset LastActivityAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt disabled at utc.
    /// </summary>
    public DateTimeOffset? DisabledAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt external identities.
    /// </summary>
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];

    /// <summary>
    /// Holt oder setzt questions.
    /// </summary>
    public ICollection<Question> Questions { get; set; } = [];

    /// <summary>
    /// Holt oder setzt study sessions.
    /// </summary>
    public ICollection<StudySession> StudySessions { get; set; } = [];
}
