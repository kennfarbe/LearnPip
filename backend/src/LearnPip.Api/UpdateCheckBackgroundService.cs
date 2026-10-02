using LearnPip.Api.Administration;

namespace LearnPip.Api;

public sealed class UpdateCheckBackgroundService(IServiceScopeFactory scopes, ILogger<UpdateCheckBackgroundService> logger)
    : BackgroundService
{
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
                logger.LogWarning(ex, "Scheduled update check failed; it will be retried.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
