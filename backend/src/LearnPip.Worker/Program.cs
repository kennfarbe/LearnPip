using LearnPip.Worker;

if (args is ["--healthcheck"])
{
    var heartbeat = Path.Combine(Path.GetTempPath(), "learnpip-worker-heartbeat");
    Environment.ExitCode = File.Exists(heartbeat) &&
        DateTime.UtcNow - File.GetLastWriteTimeUtc(heartbeat) < TimeSpan.FromMinutes(2) ? 0 : 1;
    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
