// <copyright file="AiDailyUsage.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data.Domain;

public sealed class AiDailyUsage
{
    public Guid AccountId { get; set; }

    public DateOnly Day { get; set; }

    public string Mode { get; set; } = string.Empty;

    public int UsedRequests { get; set; }
}
