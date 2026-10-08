// <copyright file="UpdateCheckTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using LearnPip.Api.Administration;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>Prüft manuelle Releaseabfragen, abgelaufene Sitzungen und erhaltene Fehlerzustände.</summary>
public sealed partial class ApiV1Tests
{
    /// <summary>Eine manuelle Abfrage ignoriert das Intervall, meldet Fehler und erholt sich nach erneutem Erfolg.</summary>
    /// <returns>Die Integrationsprüfung gegen PostgreSQL und synthetische Releaseantworten.</returns>
    [Fact]
    public async Task ManualUpdateCheckRefreshesVersionsAndRetainsFailureState()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var connection = new NpgsqlConnectionStringBuilder(source) { Database = "learnpip_updates_" + Guid.NewGuid().ToString("N") };
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        await using var server = new NpgsqlConnection(maintenance.ConnectionString);
        await server.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{connection.Database}\"", server))
        {
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<LearnPipDbContext>().UseNpgsql(connection.ConnectionString).Options;
            var owner = Guid.NewGuid();
            var session = Guid.NewGuid();
            var previous = DateTimeOffset.UtcNow;
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Accounts.Add(new Account { Id = owner });
                var role = new RoleDefinition { Scope = "system", Code = "admin", Name = "Administrator" };
                db.Roles.Add(role);
                db.AccountRoles.Add(new AccountRole { AccountId = owner, RoleDefinitionId = role.Id });
                db.AccountSessions.Add(new AccountSession { Id = session, AccountId = owner, TokenHash = new string('a', 64), CreatedAtUtc = previous.AddHours(-1), ExpiresAtUtc = previous.AddHours(1) });
                db.SystemSettings.AddRange(
                    new SystemSetting { Key = "update_last_check_utc", Value = previous.ToString("O") },
                    new SystemSetting { Key = "update_latest_version", Value = "v1.14.0" });
                await db.SaveChangesAsync();
            }

            using var handler = new UpdateReleaseHandler();
            using var factory = ExportFactory(connection.ConnectionString).WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["LearnPip:Version"] = "v1.14.0" }));
                builder.ConfigureTestServices(services => services.AddHttpClient("github-releases")
                    .ConfigurePrimaryHttpMessageHandler(() => handler));
            });
            using var client = ClientFor(factory, owner);
            client.DefaultRequestHeaders.Add("X-Test-Session", session.ToString());
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/api/v1/admin/updates/check", null)).StatusCode);
            Assert.Equal(0, handler.Calls);
            await using (var db = new LearnPipDbContext(options))
            {
                (await db.AccountSessions.SingleAsync(item => item.Id == session)).CreatedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }

            using var refreshed = await client.PostAsync("/api/v1/admin/updates/check", null);
            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
            var status = (await refreshed.Content.ReadFromJsonAsync<UpdateStatus>())!;
            Assert.Equal("v1.15.0", status.LatestVersion);
            Assert.Equal("update_available", status.State);
            Assert.True(status.LastCheckedAtUtc > previous);
            Assert.Equal(1, handler.Calls);
            var successful = status.LastCheckedAtUtc;
            foreach (var failure in new[] { "http", "timeout", "json" })
            {
                handler.Mode = failure;
                using var failed = await client.PostAsync("/api/v1/admin/updates/check", null);
                Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
                status = (await failed.Content.ReadFromJsonAsync<UpdateStatus>())!;
                Assert.Equal("check_failed", status.State);
                Assert.NotNull(status.Error);
                Assert.Equal("v1.15.0", status.LatestVersion);
                Assert.Equal(successful, status.LastCheckedAtUtc);
                var reloaded = (await client.GetFromJsonAsync<UpdateStatus>("/api/v1/admin/updates"))!;
                Assert.Equal("check_failed", reloaded.State);
                Assert.NotNull(reloaded.Error);
            }

            handler.Mode = "success";
            using var recovered = await client.PostAsync("/api/v1/admin/updates/check", null);
            status = (await recovered.Content.ReadFromJsonAsync<UpdateStatus>())!;
            Assert.Equal("update_available", status.State);
            Assert.Null(status.Error);
            Assert.Null((await client.GetFromJsonAsync<UpdateStatus>("/api/v1/admin/updates"))!.Error);
            Assert.Equal(5, handler.Calls);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{connection.Database}\" WITH (FORCE)", server);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class UpdateReleaseHandler : HttpMessageHandler
    {
        public string Mode { get; set; } = "success";

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Calls++;
            Assert.Equal("https://api.github.com/repos/kennfarbe/LearnPip/releases?per_page=20", request.RequestUri!.ToString());
            if (this.Mode == "timeout")
            {
                throw new TaskCanceledException("Synthetic HTTP timeout", new TimeoutException());
            }

            var json = this.Mode == "json" ? "invalid json" : """
                [{"tag_name":"v1.16.0-beta.1","name":"Beta","body":"","html_url":"https://github.com/kennfarbe/LearnPip/releases/tag/v1.16.0-beta.1","draft":false,"prerelease":true},
                 {"tag_name":"v1.15.0","name":"Synthetischer stabiler Stand","body":"","html_url":"https://github.com/kennfarbe/LearnPip/releases/tag/v1.15.0","draft":false,"prerelease":false}]
                """;
            return Task.FromResult(new HttpResponseMessage(this.Mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            {
                Content = new StringContent(json),
            });
        }
    }
}
