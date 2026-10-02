// <copyright file="QuestionVersion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

/// <summary>
/// Beschreibt das LearnPip-Datenmodell QuestionVersion.
/// </summary>
public sealed class QuestionVersion
{
    /// <summary>
    /// Ruft eindeutige Kennung ab oder legt den Wert fest.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ruft question id ab oder legt den Wert fest.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Ruft created by account id ab oder legt den Wert fest.
    /// </summary>
    public Guid CreatedByAccountId { get; set; }

    /// <summary>
    /// Ruft version number ab oder legt den Wert fest.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Ruft visibility ab oder legt den Wert fest.
    /// </summary>
    public string Visibility { get; set; } = "private";

    /// <summary>
    /// Ruft prompt ab oder legt den Wert fest.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Ruft explanation ab oder legt den Wert fest.
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Ruft selection mode ab oder legt den Wert fest.
    /// </summary>
    public string SelectionMode { get; set; } = "single";

    /// <summary>
    /// Ruft subject ab oder legt den Wert fest.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Ruft topic ab oder legt den Wert fest.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Ruft language ab oder legt den Wert fest.
    /// </summary>
    public string Language { get; set; } = "de";

    /// <summary>
    /// Ruft source ab oder legt den Wert fest.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Ruft license ab oder legt den Wert fest.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Ruft author attribution ab oder legt den Wert fest.
    /// </summary>
    public string AuthorAttribution { get; set; } = string.Empty;

    /// <summary>
    /// Ruft published at utc ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset PublishedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft blocks ab oder legt den Wert fest.
    /// </summary>
    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];

    /// <summary>
    /// Ruft Erstellungszeitpunkt in UTC ab oder legt den Wert fest.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Ruft question ab oder legt den Wert fest.
    /// </summary>
    public Question Question { get; set; } = null!;

    /// <summary>
    /// Ruft created by ab oder legt den Wert fest.
    /// </summary>
    public Account CreatedBy { get; set; } = null!;

    /// <summary>
    /// Ruft answer options ab oder legt den Wert fest.
    /// </summary>
    public ICollection<AnswerOption> AnswerOptions { get; set; } = [];

    /// <summary>
    /// Ruft media assets ab oder legt den Wert fest.
    /// </summary>
    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
}
