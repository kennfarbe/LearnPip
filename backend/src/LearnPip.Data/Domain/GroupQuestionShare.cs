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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt study group id.
    /// </summary>
    public Guid StudyGroupId { get; set; }

    /// <summary>
    /// Holt oder setzt question id.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Holt oder setzt shared by account id.
    /// </summary>
    public Guid SharedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt shared at utc.
    /// </summary>
    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt revoked at utc.
    /// </summary>
    public DateTimeOffset? RevokedAtUtc { get; set; }

    /// <summary>
    /// Holt oder setzt study group.
    /// </summary>
    public StudyGroup StudyGroup { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt question.
    /// </summary>
    public Question Question { get; set; } = null!;
}
