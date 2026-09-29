using LearnPip.Data;

namespace LearnPip.Worker;

public class Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LearnPip background worker started.");
        var heartbeat = WriteHeartbeat(stoppingToken);
        var nextRun = DateTimeOffset.MinValue;
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            do
            {
                if (DateTimeOffset.UtcNow < nextRun) continue;
                nextRun = DateTimeOffset.UtcNow.AddHours(1);
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var result = await scope.ServiceProvider.GetRequiredService<AccountLifecycleService>()
                        .RunOnceAsync(DateTimeOffset.UtcNow, stoppingToken);
                    logger.LogInformation("Account lifecycle: {Warnings} warnings, {Deactivated} deactivated, {Deleted} deleted.",
                        result.WarningsClaimed, result.Deactivated, result.Deleted);
                    nextRun = DateTimeOffset.UtcNow.AddDays(1);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Account lifecycle run failed; retrying in one hour.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            try { await heartbeat; }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }
    }

    private static async Task WriteHeartbeat(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        do
        {
            await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "learnpip-worker-heartbeat"),
                DateTimeOffset.UtcNow.ToString("O"), stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
