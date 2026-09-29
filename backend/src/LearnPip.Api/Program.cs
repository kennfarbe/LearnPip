using System.Threading.RateLimiting;
using LearnPip.Api;
using LearnPip.Api.Identity;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LearnPip")
    ?? throw new InvalidOperationException("ConnectionStrings:LearnPip must be configured.");
builder.Services.AddDbContext<LearnPipDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddOpenApi("v1");
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);

builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddSingleton<IEmailCodeSender, SmtpEmailCodeSender>();
builder.Services.AddAuthentication(SessionAuthentication.Scheme)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        SessionAuthenticationHandler>(SessionAuthentication.Scheme, _ => { })
    .AddLearnPipOidc(builder.Configuration);
builder.Services.AddApiAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method) &&
        context.Request.Cookies.ContainsKey(SessionAuthentication.CookieName) &&
        !context.Request.Headers.ContainsKey("Authorization"))
    {
        var configuredOrigin = builder.Configuration["Authentication:PublicOrigin"];
        var expectedOrigin = string.IsNullOrWhiteSpace(configuredOrigin)
            ? $"{context.Request.Scheme}://{context.Request.Host}"
            : configuredOrigin;
        if (!string.Equals(context.Request.Headers.Origin.ToString(), expectedOrigin,
                StringComparison.OrdinalIgnoreCase))
        {
            await Results.Problem("Invalid request origin.",
                statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
            return;
        }
    }

    await next();
});
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
app.MapAuthEndpoints();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LearnPipDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

app.Run();

public partial class Program;
