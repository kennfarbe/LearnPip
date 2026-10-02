// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.

namespace LearnPip.Data.Domain;

public sealed class ExamProfileVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string AmateurClass { get; set; } = string.Empty;

    public int Version { get; set; }

    public Guid CatalogEditionId { get; set; }

    public OfficialCatalogEdition CatalogEdition { get; set; } = null!;

    public string PartsJson { get; set; } = string.Empty;

    public string ScheduleJson { get; set; } = "[]";

    public string? RulesSourceUrl { get; set; }

    public DateOnly? RulesCheckedOn { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
