// <copyright file="AccountExamCredit.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell AccountExamCredit.
/// </summary>
public sealed class AccountExamCredit
{
    /// <summary>
    /// Holt oder setzt Kennung des Kontos.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Holt oder setzt fachlicher Code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt reported at utc.
    /// </summary>
    public DateTimeOffset ReportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
