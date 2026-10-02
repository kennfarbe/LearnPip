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
    /// Ruft Kennung des Kontos ab oder legt den Wert fest.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Ruft fachlicher Code ab oder legt den Wert fest.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Ruft reported at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset ReportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
