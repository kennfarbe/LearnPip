// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class GroupVersionShare
{
    public Guid StudyGroupId { get; set; }

    public Guid QuestionVersionId { get; set; }

    public Guid PrivateCatalogId { get; set; }

    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public StudyGroup StudyGroup { get; set; } = null!;

    public QuestionVersion QuestionVersion { get; set; } = null!;

    public PrivateCatalog PrivateCatalog { get; set; } = null!;
}
