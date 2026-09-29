namespace LearnPip.Worker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("LearnPip background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Background jobs are introduced by later product issues.
            await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), "learnpip-worker-heartbeat"),
                DateTimeOffset.UtcNow.ToString("O"), stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
