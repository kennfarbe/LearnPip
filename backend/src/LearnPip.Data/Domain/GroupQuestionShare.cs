// <copyright file="GroupQuestionShare.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell GroupQuestionShare.
/// </summary>
public sealed class GroupQuestionShare
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft study group id ab oder legt den Wert fest.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Ruft question id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Ruft shared by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid SharedByAccountId { get; set; }

    /// <summary>
    /// Ruft shared at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft revoked at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Ruft study group ab oder legt den Wert fest.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;

    /// <summary>
    /// Ruft question ab oder legt den Wert fest.
    /// </summary>
    public Question Question { get; set; } = null!;
}
