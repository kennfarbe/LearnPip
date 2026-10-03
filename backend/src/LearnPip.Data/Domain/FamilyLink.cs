// <copyright file="FamilyLink.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell FamilyLink.
/// </summary>
public sealed class FamilyLink
{
    /// <summary>
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt child account id.
    /// </summary>
    public Guid ChildAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt parent account id.
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt invite hash.
    /// </summary>
    public string InviteHash { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt invite expires at utc.
    /// </summary>
    public DateTimeOffset InviteExpiresAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt Bearbeitungsstatus.
    /// </summary>
    public string Status { get; set; } = "invited";

    /// <summary>
    /// Holt oder setzt verified by account id.
    /// </summary>
    public Guid? VerifiedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt verification reference.
    /// </summary>
    public string? VerificationReference { get; set; }

    /// <summary>
    /// Holt oder setzt verified at utc.
    /// </summary>
    public DateTimeOffset? VerifiedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt activated at utc.
    /// </summary>
    public DateTimeOffset? ActivatedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt revoked at utc.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt revoked by account id.
    /// </summary>
    public Guid? RevokedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
