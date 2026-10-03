// <copyright file="IdentityFlowTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LearnPip.Api;
using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für die Anmeldewege.
/// </summary>
public sealed class IdentityFlowTests
{
    /// <summary>
    /// Prüft die Verknüpfung mehrerer Anmeldewege mit einem Konto und den sofortigen Sitzungswiderruf.
    /// </summary>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    [Fact]
    public async Task Identity_paths_link_to_one_account_and_revocation_is_immediate()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var databaseName = $"learnpip_identity_test_{Guid.NewGuid():N}";
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        var connection = new NpgsqlConnectionStringBuilder(source) { Database = databaseName };

        await using (var admin = new NpgsqlConnection(maintenance.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<LearnPipDbContext>()
                .UseNpgsql(connection.ConnectionString).Options;
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
            }

            var mail = new FakeEmailSender();
            var key = Convert.ToBase64String(Enumerable.Repeat((byte)42, 32).ToArray());
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((_, configuration) =>
                        configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Authentication:EmailCodeKey"] = key,
                        }));
                    webBuilder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<LearnPipDbContext>>();
                        services.RemoveAll<IDbContextOptionsConfiguration<LearnPipDbContext>>();
                        services.AddDbContext<LearnPipDbContext>(dbOptions =>
                            dbOptions.UseNpgsql(connection.ConnectionString));
                        services.RemoveAll<IEmailCodeSender>();
                        services.AddSingleton<IEmailCodeSender>(mail);
                    });
                });

            using var anonymous = factory.CreateClient();
            using (var scope = factory.Services.CreateScope())
            {
                var apiDb = scope.ServiceProvider.GetRequiredService<LearnPipDbContext>();
                Assert.Equal(databaseName, apiDb.Database.GetDbConnection().Database);
                var actualResult1 = scope.ServiceProvider.GetRequiredService<IConfiguration>()[
                    "Authentication:EmailCodeKey"];
                Assert.Equal(
                    key,
                    actualResult1);
            }

            var createdResponse = await anonymous.PostAsJsonAsync("/api/v1/auth/pseudonymous", new { });
            Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
            var created = (await createdResponse.Content.ReadFromJsonAsync<ApiResponse<NewAccount>>())!.Data;
            Assert.Equal(43, created.RecoverySecret.Length);
            Assert.Equal(43, created.Session.Token.Length);
            using var first = ClientFor(factory, created.Session.Token);
            Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/api/v1/auth/me")).StatusCode);

            await using (var db = new LearnPipDbContext(options))
            {
                Assert.NotEqual(
                    created.RecoverySecret,
                    (await db.RecoveryCredentials.SingleAsync()).SecretHash);
                Assert.NotEqual(
                    created.Session.Token,
                    (await db.AccountSessions.SingleAsync()).TokenHash);
                db.Questions.Add(new Question { OwnerAccountId = created.AccountId });
                await db.SaveChangesAsync();
            }

            var oldActivity = DateTimeOffset.UtcNow.AddDays(-40);
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Accounts.Where(x => x.Id == created.AccountId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastActivityAtUtc, oldActivity));
            }

            Assert.Equal(
                HttpStatusCode.NotFound,
                (await first.GetAsync($"/api/v1/questions/{Guid.NewGuid()}")).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                Assert.Equal(
                    oldActivity.ToUnixTimeSeconds(),
                    (await db.Accounts.SingleAsync(x => x.Id == created.AccountId))
                    .LastActivityAtUtc.ToUnixTimeSeconds());
            }

            Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/api/v1/auth/me")).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                Assert.True((await db.Accounts.SingleAsync(x => x.Id == created.AccountId))
                    .LastActivityAtUtc > oldActivity);
            }

            var expectedResult5 = HttpStatusCode.Unauthorized;
            var actualResult6 = (await anonymous.PostAsJsonAsync(
                "/api/v1/auth/recovery",
                new RecoveryRequest("a-group-code-is-not-a-login"))).StatusCode;

            Assert.Equal(
                expectedResult5,
                actualResult6);
            var recoveredResponse = await anonymous.PostAsJsonAsync(
                "/api/v1/auth/recovery",
                new RecoveryRequest(created.RecoverySecret));
            Assert.Equal(HttpStatusCode.OK, recoveredResponse.StatusCode);
            var recovered = (await recoveredResponse.Content
                .ReadFromJsonAsync<ApiResponse<SessionGrant>>())!.Data;

            Assert.Equal(
                HttpStatusCode.NoContent,
                (await first.PostAsync("/api/v1/auth/logout", null)).StatusCode);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await first.GetAsync("/api/v1/auth/me")).StatusCode);
            using var current = ClientFor(factory, recovered.Token);
            Assert.Equal(HttpStatusCode.OK, (await current.GetAsync("/api/v1/auth/me")).StatusCode);

            var rotateResponse = await current.PostAsync("/api/v1/auth/recovery/rotate", null);
            Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
            var rotated = (await rotateResponse.Content.ReadFromJsonAsync<ApiResponse<NewAccount>>())!.Data;
            Assert.NotEqual(created.RecoverySecret, rotated.RecoverySecret);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await current.GetAsync("/api/v1/auth/me")).StatusCode);
            var expectedResult7 = HttpStatusCode.Unauthorized;
            var actualResult8 = (await anonymous.PostAsJsonAsync(
                "/api/v1/auth/recovery",
                new RecoveryRequest(created.RecoverySecret))).StatusCode;
            Assert.Equal(
                expectedResult7,
                actualResult8);

            using var linked = ClientFor(factory, rotated.Session.Token);
            var expectedResult9 = HttpStatusCode.Accepted;
            var actualResult10 = (await linked.PostAsJsonAsync(
                "/api/v1/auth/email/link/start",
                new EmailStartRequest("Student@example.org"))).StatusCode;
            Assert.Equal(
                expectedResult9,
                actualResult10);
            var linkCode = mail.Code;
            Assert.Equal(6, linkCode.Length);
            var expectedResult11 = HttpStatusCode.Unauthorized;
            var actualResult12 = (await linked.PostAsJsonAsync(
                "/api/v1/auth/email/link/complete",
                new EmailCompleteRequest(
                    "student@example.org",
                    linkCode == "000000" ? "111111" : "000000"))).StatusCode;
            Assert.Equal(
                expectedResult11,
                actualResult12);
            var expectedResult13 = HttpStatusCode.NoContent;
            var actualResult14 = (await linked.PostAsJsonAsync(
                "/api/v1/auth/email/link/complete",
                new EmailCompleteRequest("student@example.org", linkCode))).StatusCode;
            Assert.Equal(
                expectedResult13,
                actualResult14);
            var expectedResult15 = HttpStatusCode.Unauthorized;
            var actualResult16 = (await linked.PostAsJsonAsync(
                "/api/v1/auth/email/link/complete",
                new EmailCompleteRequest("student@example.org", linkCode))).StatusCode;
            Assert.Equal(
                expectedResult15,
                actualResult16);
            var expectedResult17 = HttpStatusCode.Accepted;
            var actualResult18 = (await anonymous.PostAsJsonAsync(
                "/api/v1/auth/email/start",
                new EmailStartRequest("student@example.org"))).StatusCode;

            Assert.Equal(
                expectedResult17,
                actualResult18);
            var signInCode = mail.Code;
            var emailResponse = await anonymous.PostAsJsonAsync(
                "/api/v1/auth/email/complete",
                new EmailCompleteRequest("student@example.org", signInCode));
            Assert.Equal(HttpStatusCode.OK, emailResponse.StatusCode);
            var emailSession = (await emailResponse.Content
                .ReadFromJsonAsync<ApiResponse<SessionGrant>>())!.Data;
            var expectedResult19 = HttpStatusCode.Unauthorized;
            var actualResult20 = (await anonymous.PostAsJsonAsync(
                "/api/v1/auth/email/complete",
                new EmailCompleteRequest("student@example.org", signInCode))).StatusCode;
            Assert.Equal(
                expectedResult19,
                actualResult20);

            using (var scope = factory.Services.CreateScope())
            {
                var identity = scope.ServiceProvider.GetRequiredService<IdentityService>();
                var oidcAccount = await identity.ResolveOidcAsync(
                    "https://identity.example.org",
                    "subject-1",
                    created.AccountId,
                    CancellationToken.None);
                Assert.Equal(created.AccountId, oidcAccount);
                var expectedResult2 = created.AccountId;
                var actualResult3 = await identity.ResolveOidcAsync(
                    "https://identity.example.org",
                    "subject-1",
                    null,
                    CancellationToken.None);
                Assert.Equal(
                    expectedResult2,
                    actualResult3);
                await Assert.ThrowsAsync<IdentityConflictException>(() =>
                    identity.ResolveOidcAsync(
                        "https://identity.example.org",
                        "subject-1",
                        Guid.NewGuid(),
                        CancellationToken.None));
            }

            await using (var db = new LearnPipDbContext(options))
            {
                Assert.Equal(
                    created.AccountId,
                    (await db.ExternalIdentities.SingleAsync(item => item.Provider == "email")).AccountId);
                var actualResult4 = await db.Questions.CountAsync(item =>
                    item.OwnerAccountId == created.AccountId);
                Assert.Equal(
                    1,
                    actualResult4);
                Assert.Equal(1, await db.Accounts.CountAsync());
                Assert.All(
                    await db.EmailLoginCodes.ToListAsync(),
                    item =>
                    Assert.NotEqual(linkCode, item.CodeHash));
            }

            using var emailClient = ClientFor(factory, emailSession.Token);
            Assert.Equal(HttpStatusCode.OK, (await emailClient.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await emailClient.PostAsync("/api/v1/auth/logout-all", null)).StatusCode);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await emailClient.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await linked.GetAsync("/api/v1/auth/me")).StatusCode);

            var limited = false;
            for (var attempt = 0; attempt < 25; attempt++)
            {
                var response = await anonymous.PostAsJsonAsync(
                    "/api/v1/auth/recovery",
                    new RecoveryRequest("invalid"));
                limited |= response.StatusCode == HttpStatusCode.TooManyRequests;
            }

            Assert.True(limited);
        }
        finally
        {
            await using var admin = new NpgsqlConnection(maintenance.ConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @name AND pid <> pg_backend_pid()",
                admin))
            {
                terminate.Parameters.AddWithValue("name", databaseName);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static HttpClient ClientFor(
        WebApplicationFactory<Program> factory,
        string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed class FakeEmailSender : IEmailCodeSender
    {
        public bool IsAvailable => true;

        public string Code { get; private set; } = string.Empty;

        public Task SendAsync(
            string email,
            string code,
            CancellationToken cancellationToken)
        {
            this.Code = code;
            return Task.CompletedTask;
        }
    }
}
