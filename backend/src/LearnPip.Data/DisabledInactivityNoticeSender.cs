// <copyright file="DisabledInactivityNoticeSender.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

namespace LearnPip.Data;

/// <summary>
/// Deaktiviert den optionalen Inaktivitätsversand ohne Seiteneffekte.
/// </summary>
public sealed class DisabledInactivityNoticeSender : IInactivityNoticeSender
{
    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <inheritdoc />
    public Task SendAsync(
        string email,
        int phaseDays,
        DateTimeOffset lastActivityAtUtc,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
