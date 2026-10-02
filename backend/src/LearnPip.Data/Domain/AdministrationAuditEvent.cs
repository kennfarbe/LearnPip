// <copyright file="AdministrationAuditEvent.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AdministrationAuditEvent.
/// </summary>
public sealed class AdministrationAuditEvent
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt actor account id.
    /// </summary>
    public Guid? ActorAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt action.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt target.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt previous value.
    /// </summary>
    public string? PreviousValue { get; set; }

    /// <summary>
    /// Holt oder setzt new value.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
