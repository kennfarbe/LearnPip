// <copyright file="OfficialCatalogEdition.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class OfficialCatalogEdition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Revision { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string License { get; set; } = string.Empty;

    public string Attribution { get; set; } = string.Empty;

    public DateOnly ChangedOn { get; set; }

    public DateTimeOffset ImportedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public string QuestionsJson { get; set; } = string.Empty;
}
