// <copyright file="UpdateCheckBackgroundService.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Api.Administration;

namespace LearnPip.Api;

/// <summary>
/// Prüft im Hintergrund auf neue stabile Versionen.
/// </summary>
/// <param name="scopes">Die Factory für abgegrenzte Dienstbereiche.</param>
/// <param name="logger">Der Logger für den Dienst.</param>
public sealed class UpdateCheckBackgroundService(
        IServiceScopeFactory scopes,
        ILogger<UpdateCheckBackgroundService> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> CheckFailedLog = LoggerMessage.Define(
        LogLevel.Warning,
        new EventId(1, "ScheduledCheckFailed"),
        "Scheduled update check failed; it will be retried.");

    /// <summary>
    /// Führt die Hintergrundarbeit bis zum Abbruch des Hosts aus.
    /// </summary>
    /// <param name="stoppingToken">Das Token zum Beenden des Hintergrunddiensts.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<UpdateService>().CheckScheduledAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                CheckFailedLog(logger, ex);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
