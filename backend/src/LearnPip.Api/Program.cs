using LearnPip.Api;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LearnPip")
    ?? throw new InvalidOperationException("ConnectionStrings:LearnPip must be configured.");
builder.Services.AddDbContext<LearnPipDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddOpenApi("v1");
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);

var authority = builder.Configuration["Authentication:Authority"];
var audience = builder.Configuration["Authentication:Audience"];
if (string.IsNullOrWhiteSpace(authority) != string.IsNullOrWhiteSpace(audience))
{
    throw new InvalidOperationException("Configure both Authentication:Authority and Authentication:Audience.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrWhiteSpace(authority))
        {
            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = true;
        }
    });
builder.Services.AddApiAuthorization();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
    .WithName("Liveness");
app.MapGet("/health/ready", async (LearnPipDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .WithName("Readiness");
app.MapV1Endpoints();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LearnPipDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.Run();

public partial class Program;
