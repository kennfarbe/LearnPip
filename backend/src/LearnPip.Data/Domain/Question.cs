// <copyright file="Question.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerAccountId { get; set; }

    public Guid? LearningContentId { get; set; }

    public LearningContent? LearningContent { get; set; }

    public Guid? PrivateCatalogId { get; set; }

    public PrivateCatalog? PrivateCatalog { get; set; }

    public QuestionDraft? Draft { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Account Owner { get; set; } = null!;

    public ICollection<QuestionVersion> Versions { get; set; } = [];
}
