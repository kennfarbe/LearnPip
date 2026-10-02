// <copyright file="QuestionVersion.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class QuestionVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuestionId { get; set; }

    public Guid CreatedByAccountId { get; set; }

    public int VersionNumber { get; set; }

    public string Visibility { get; set; } = "private";

    public string Prompt { get; set; } = string.Empty;

    public string? Explanation { get; set; }

    public string SelectionMode { get; set; } = "single";

    public string Subject { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Language { get; set; } = "de";

    public string Source { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string AuthorAttribution { get; set; } = string.Empty;

    public DateTimeOffset PublishedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Question Question { get; set; } = null!;

    public Account CreatedBy { get; set; } = null!;

    public ICollection<AnswerOption> AnswerOptions { get; set; } = [];

    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
}
