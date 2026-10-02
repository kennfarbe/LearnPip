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
    /// Holt oder setzt eindeutige Kennung.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Holt oder setzt question id.
    /// </summary>
    public Guid QuestionId { get; set; }

    /// <summary>
    /// Holt oder setzt created by account id.
    /// </summary>
    public Guid CreatedByAccountId { get; set; }

    /// <summary>
    /// Holt oder setzt version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Holt oder setzt visibility.
    /// </summary>
    public string Visibility { get; set; } = "private";

    /// <summary>
    /// Holt oder setzt prompt.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt explanation.
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Holt oder setzt selection mode.
    /// </summary>
    public string SelectionMode { get; set; } = "single";

    /// <summary>
    /// Holt oder setzt subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt topic.
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt language.
    /// </summary>
    public string Language { get; set; } = "de";

    /// <summary>
    /// Holt oder setzt source.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt license.
    /// </summary>
    public string License { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt author attribution.
    /// </summary>
    public string AuthorAttribution { get; set; } = string.Empty;

    /// <summary>
    /// Holt oder setzt published at utc.
    /// </summary>
    public DateTimeOffset PublishedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt blocks.
    /// </summary>
    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];

    /// <summary>
    /// Holt oder setzt Erstellungszeitpunkt in UTC.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Holt oder setzt question.
    /// </summary>
    public Question Question { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt created by.
    /// </summary>
    public Account CreatedBy { get; set; } = null!;

    /// <summary>
    /// Holt oder setzt answer options.
    /// </summary>
    public ICollection<AnswerOption> AnswerOptions { get; set; } = [];

    /// <summary>
    /// Holt oder setzt media assets.
    /// </summary>
    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
}
