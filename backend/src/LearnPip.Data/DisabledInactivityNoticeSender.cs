// <copyright file="DisabledInactivityNoticeSender.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data;

public sealed class DisabledInactivityNoticeSender : IInactivityNoticeSender
{
    public bool IsAvailable => false;

    public Task SendAsync(
        string email,
        int phaseDays,
        DateTimeOffset lastActivityAtUtc,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
