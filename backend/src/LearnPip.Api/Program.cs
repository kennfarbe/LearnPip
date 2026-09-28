using LearnPip.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LearnPip")
    ?? throw new InvalidOperationException("ConnectionStrings:LearnPip must be configured.");
builder.Services.AddDbContext<LearnPipDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .WithName("Liveness");
app.MapGet("/health/ready", async (LearnPipDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .WithName("Readiness");

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LearnPipDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.Run();
