// <copyright file="Program.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Threading.RateLimiting;
using LearnPip.Api;
using LearnPip.Api.Administration;
using LearnPip.Api.Ai;
using LearnPip.Api.CatalogPackages;
using LearnPip.Api.Exams;
using LearnPip.Api.Family;
using LearnPip.Api.Groups;
using LearnPip.Api.Identity;
using LearnPip.Api.Media;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

if (args is ["--healthcheck"])
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    try
    {
        using var response = await client.GetAsync(new UriBuilder(Uri.UriSchemeHttp, System.Net.IPAddress.Loopback.ToString(), 8080, "health/ready").Uri);
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException)
    {
        Environment.ExitCode = 1;
    }
    catch (TaskCanceledException)
    {
        Environment.ExitCode = 1;
    }

    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);
if (builder.Environment.IsProduction())
{
    const string keyDirectory = "/var/lib/learnpip/data-protection";
    if (!Directory.Exists(keyDirectory))
    {
        throw new InvalidOperationException("Persistent Data Protection key storage is required.");
    }

    builder.Services.AddDataProtection()
        .SetApplicationName("LearnPip.Api")
        .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));
}

var connectionString = builder.Configuration.GetConnectionString("LearnPip")
    ?? throw new InvalidOperationException("ConnectionStrings:LearnPip must be configured.");
builder.Services.AddDbContext<LearnPipDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddOpenApi("v1");
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);

builder.Services.AddScoped<IdentityService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AccountLifecycleService>();
builder.Services.AddSingleton<IInactivityNoticeSender, DisabledInactivityNoticeSender>();
builder.Services.AddScoped<AdministrationService>();
builder.Services.AddScoped<UpdateService>();
builder.Services.AddHttpClient("github-releases", client => client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHostedService<UpdateCheckBackgroundService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<PublicSubmissionService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddSingleton<AiGateway>();
builder.Services.AddScoped<IPrivateMediaStore, PostgresPrivateMediaStore>();
builder.Services.AddSingleton<IEmailCodeSender, SmtpEmailCodeSender>();
builder.Services.AddAuthentication(SessionAuthentication.Scheme)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        SessionAuthenticationHandler>(
    SessionAuthentication.Scheme,
    _ => { })
    .AddLearnPipOidc(builder.Configuration)
    .AddLearnPipGithub(builder.Configuration)
    .AddLearnPipFacebook(builder.Configuration);
builder.Services.AddApiAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
            "auth",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    options.AddPolicy(
            "account-delete",
            context => RateLimitPartition.GetFixedWindowLimiter(
                AccountIdentity.TryGetAccountId(
                    context.User,
                    out var accountId)
            ? accountId.ToString() : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 3,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    options.AddPolicy(
            "data-export",
            context => RateLimitPartition.GetFixedWindowLimiter(
                AccountIdentity.TryGetAccountId(
                    context.User,
                    out var accountId)
            ? accountId.ToString() : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    options.AddPolicy(
            "content-write",
            context => RateLimitPartition.GetFixedWindowLimiter(
                AccountIdentity.TryGetAccountId(
                    context.User,
                    out var accountId)
            ? accountId.ToString() : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 30,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    options.AddPolicy(
            "update-admin",
            context => RateLimitPartition.GetFixedWindowLimiter(
                AccountIdentity.TryGetAccountId(
                    context.User,
                    out var accountId)
            ? accountId.ToString() : context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 6,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
    options.AddPolicy(
            "group-join",
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRouting();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1/ai") &&
        context.Request.Method is "PUT" or "POST")
    {
        var limit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
        {
            limit.MaxRequestBodySize = 65536;
        }
    }

    await next();
});
app.UseAuthentication();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method) &&
        (context.Request.Cookies.ContainsKey(SessionAuthentication.CookieName) ||
         context.Request.Path == "/api/v1/auth/password") &&
        !context.Request.Headers.ContainsKey("Authorization"))
    {
        var configuredOrigin = builder.Configuration["Authentication:PublicOrigin"];
        var expectedOrigin = string.IsNullOrWhiteSpace(configuredOrigin)
            ? $"{context.Request.Scheme}://{context.Request.Host}"
            : configuredOrigin;
        if (!string.Equals(
                context.Request.Headers.Origin.ToString(),
                expectedOrigin,
                StringComparison.OrdinalIgnoreCase))
        {
            await Results.Problem(
                    "Invalid request origin.",
                    statusCode: StatusCodes.Status403Forbidden).ExecuteAsync(context);
            return;
        }
    }

    await next();
});
app.UseAuthorization();
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode is < 200 or >= 300 ||
        !context.Request.Path.StartsWithSegments("/api/v1") ||
        context.User.Identity?.IsAuthenticated != true ||
        !AccountIdentity.TryGetAccountId(context.User, out var accountId))
    {
        return;
    }

    var db = context.RequestServices.GetRequiredService<LearnPipDbContext>();
    var now = DateTimeOffset.UtcNow;
    await db.Accounts.Where(account => account.Id == accountId && account.DeletedAtUtc == null &&
            account.DisabledAtUtc == null)
        .ExecuteUpdateAsync(
            setters => setters
            .SetProperty(
                account => account.LastActivityAtUtc,
                now)
            .SetProperty(account => account.UpdatedAtUtc, now),
            context.RequestAborted);
});
app.MapOpenApi();

app.MapGet(
    "/health/live",
    () => Results.Ok(new { status = "live" }))
    .WithName("Liveness");
app.MapGet(
    "/health/ready",
    async (LearnPipDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .WithName("Readiness");
app.MapV1Endpoints();
app.MapAuthEndpoints();
app.MapPasswordEndpoints();
app.MapDataRightsEndpoints();
app.MapMediaEndpoints();
app.MapQuestionEndpoints();
app.MapTranslationEndpoints();
app.MapPublicSubmissionEndpoints();
app.MapCommunityFeedbackEndpoints();
app.MapLearningSessionEndpoints();
app.MapReviewEndpoints();
app.MapProgressEndpoints();
app.MapFamilyEndpoints();
app.MapExamPlanEndpoints();
app.MapExamEndpoints();
app.MapAiEndpoints();
app.MapCatalogEditorEndpoints();
app.MapCatalogPackageEndpoints();
app.MapCatalogExportEndpoints();
app.MapGroupEndpoints();
app.MapAdministrationEndpoints();
app.MapQuestionPermissionEndpoints();
app.MapQuestionManagementEndpoints();

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LearnPipDbContext>();
    await dbContext.Database.MigrateAsync();
    return;
}

if (args is ["--initialize-admin"] or ["--reset-admin-password"])
{
    // Two lines over stdin; never command-line arguments or long-lived configuration.
    var username = await Console.In.ReadLineAsync() ?? string.Empty;
    var password = await Console.In.ReadLineAsync() ?? string.Empty;
    await using var scope = app.Services.CreateAsyncScope();
    var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
    if (args[0] == "--reset-admin-password")
    {
        await passwords.ResetAsync(username, password);
        Console.WriteLine("Passwort zurückgesetzt; bestehende Sitzungen widerrufen.");
    }
    else
    {
        var created = await passwords.BootstrapAsync(username, password);
        Console.WriteLine(created ? "Administrator eingerichtet." : "Administrator bereits eingerichtet; unverändert.");
        Environment.ExitCode = created ? 0 : 10;
    }

    return;
}

if (args.Contains("--bootstrap-admin", StringComparer.OrdinalIgnoreCase))
{
    if (args.Length != 1 ||
        !Guid.TryParse(builder.Configuration["Authentication:BootstrapAdminAccountId"], out var accountId))
    {
        throw new InvalidOperationException("Set Authentication__BootstrapAdminAccountId to an existing account UUID and pass only --bootstrap-admin.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AdministrationService>().BootstrapAsync(accountId);
    return;
}

await app.RunAsync();

/// <summary>
/// Einstiegspunkt und Konfiguration des API-Hosts.
/// </summary>
public partial class Program
{
    private Program()
    {
        // Die Instanz wird nicht benötigt; der API-Host verwendet den statischen Einstiegspunkt.
    }
}
