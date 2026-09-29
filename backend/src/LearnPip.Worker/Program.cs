using LearnPip.Data;
using LearnPip.Worker;
using Microsoft.EntityFrameworkCore;

if (args is ["--healthcheck"])
{
    var heartbeat = Path.Combine(Path.GetTempPath(), "learnpip-worker-heartbeat");
    Environment.ExitCode = File.Exists(heartbeat) &&
        DateTime.UtcNow - File.GetLastWriteTimeUtc(heartbeat) < TimeSpan.FromMinutes(2) ? 0 : 1;
    return;
}

var runOnce = args is ["--run-once"];
var builder = Host.CreateApplicationBuilder(runOnce ? [] : args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
var connectionString = builder.Configuration.GetConnectionString("LearnPip")
    ?? throw new InvalidOperationException("ConnectionStrings:LearnPip must be configured.");
builder.Services.AddDbContext<LearnPipDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<AccountLifecycleService>();
builder.Services.AddSingleton<IInactivityNoticeSender, SmtpInactivityNoticeSender>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
if (runOnce)
{
    await using var scope = host.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AccountLifecycleService>()
        .RunOnceAsync(DateTimeOffset.UtcNow);
    return;
}
host.Run();
