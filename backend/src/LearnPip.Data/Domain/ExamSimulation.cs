// <copyright file="ExamSimulation.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class ExamSimulation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }

    public Guid ProfileVersionId { get; set; }

    public ExamProfileVersion ProfileVersion { get; set; } = null!;

    public string SnapshotJson { get; set; } = string.Empty;

    public string AnswersJson { get; set; } = "{}";

    public int CurrentPartIndex { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset PartStartedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? ResultJson { get; set; }
}
