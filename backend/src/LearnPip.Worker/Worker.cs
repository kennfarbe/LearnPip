// <copyright file="Worker.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;

namespace LearnPip.Worker;

/// <summary>
/// Führt Lernimpulse, Kontolebenszyklus und den Heartbeat des Workers aus.
/// </summary>
/// <param name="logger">Der Logger des Workers.</param>
/// <param name="scopeFactory">Die Factory für abgegrenzte Dienstbereiche.</param>
public class Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private static readonly Action<ILogger, Exception?> StartedLog = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(1, "WorkerStarted"),
        "LearnPip background worker started.");

    private static readonly Action<ILogger, int, Exception?> RemindersSentLog = LoggerMessage.Define<int>(
        LogLevel.Information,
        new EventId(2, "RemindersSent"),
        "Learning reminders sent: {Count}.");

    private static readonly Action<ILogger, Exception?> ReminderFailedLog = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(3, "ReminderFailed"),
        "Learning reminder run failed.");

    private static readonly Action<ILogger, int, int, int, Exception?> LifecycleLog = LoggerMessage.Define<int, int, int>(
        LogLevel.Information,
        new EventId(4, "AccountLifecycle"),
        "Account lifecycle: {Warnings} warnings, {Deactivated} deactivated, {Deleted} deleted.");

    private static readonly Action<ILogger, Exception?> LifecycleFailedLog = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(5, "LifecycleFailed"),
        "Account lifecycle run failed; retrying in one hour.");

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartedLog(logger, null);
        var heartbeat = WriteHeartbeat(stoppingToken);
        var nextRun = DateTimeOffset.MinValue;
        var nextReminderRun = DateTimeOffset.MinValue;
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            do
            {
                if (DateTimeOffset.UtcNow >= nextReminderRun)
                {
                    nextReminderRun = DateTimeOffset.UtcNow.AddHours(1);
                    try
                    {
                        await using var reminderScope = scopeFactory.CreateAsyncScope();
                        var count = await reminderScope.ServiceProvider.GetRequiredService<LearningReminderService>()
                            .RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken);
                        RemindersSentLog(logger, count, null);
                    }
                    catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        ReminderFailedLog(logger, exception);
                    }
                }

                if (DateTimeOffset.UtcNow < nextRun)
                {
                    continue;
                }

                nextRun = DateTimeOffset.UtcNow.AddHours(1);
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<AccountLifecycleService>()
                        .RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken);
                    LifecycleLog(logger, result.WarningsClaimed, result.Deactivated, result.Deleted, null);
                    nextRun = DateTimeOffset.UtcNow.AddDays(1);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    LifecycleFailedLog(logger, exception);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            try
            {
                await heartbeat;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Der Heartbeat endet beim regulären Herunterfahren des Hosts.
            }
        }
    }

    private static async Task WriteHeartbeat(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            await File.WriteAllTextAsync(
                Path.Combine(Path.GetTempPath(), "learnpip-worker-heartbeat"),
                DateTimeOffset.UtcNow.ToString("O"),
                stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
