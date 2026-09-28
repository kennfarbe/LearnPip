var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .WithName("Liveness");
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
    .WithName("Readiness");

app.Run();
