namespace LearnPip.Worker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LearnPip background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Background jobs are introduced by later product issues.
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
