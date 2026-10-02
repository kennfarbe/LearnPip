// <copyright file="IInactivityNoticeSender.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data;

public interface IInactivityNoticeSender
{
    bool IsAvailable { get; }

    Task SendAsync(
        string email,
        int phaseDays,
        DateTimeOffset lastActivityAtUtc,
        CancellationToken cancellationToken);
}
