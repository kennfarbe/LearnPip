// <copyright file="PasswordFlowTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using LearnPip.Api;
using LearnPip.Api.Identity;
using LearnPip.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>Prüft Bootstrap, lokale Passwortzugänge, CSRF und sofortigen Sitzungswiderruf.</summary>
public sealed class PasswordFlowTests
{
    private const string InitialPassword = "synthetic initial password";
    private const string ChangedPassword = "synthetic changed password";

    /// <summary>Prüft nebenläufigen Bootstrap und sämtliche Passwort-Lebenszyklen in PostgreSQL.</summary>
    /// <returns>Die asynchrone Testoperation.</returns>
    [Fact]
    public async Task BootstrapAndPasswordLifecycleRemainTransactional()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Disposable PostgreSQL required.");
        var name = $"learnpip_password_test_{Guid.NewGuid():N}";
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        var connection = new NpgsqlConnectionStringBuilder(source) { Database = name };
        await using (var admin = new NpgsqlConnection(maintenance.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<LearnPipDbContext>().UseNpgsql(connection.ConnectionString).Options;
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
            }

            async Task<bool> Bootstrap()
            {
                await using var db = new LearnPipDbContext(options);
                return await new PasswordService(db, new SessionService(db)).BootstrapAsync("Admin", InitialPassword);
            }

            var results = await Task.WhenAll(Bootstrap(), Bootstrap());
            Assert.Single(results, value => value);
            await using (var db = new LearnPipDbContext(options))
            {
                var service = new PasswordService(db, new SessionService(db));
                Assert.False(await service.BootstrapAsync("other", "invalid"));
                Assert.Equal(1, await db.Accounts.CountAsync());
                Assert.Equal(1, await db.AccountRoles.CountAsync());
                Assert.Equal(1, await db.AdministrationAuditEvents.CountAsync());
                var credential = await db.PasswordCredentials.SingleAsync();
                Assert.Equal("admin", credential.Username);
                Assert.NotEqual(InitialPassword, credential.PasswordHash);
                Assert.Null(await service.SignInAsync("unknown", InitialPassword, CancellationToken.None));
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    Assert.Null(await service.SignInAsync("admin", "wrong", CancellationToken.None));
                }

                Assert.Null(await service.SignInAsync("admin", InitialPassword, CancellationToken.None));
                await service.ResetAsync("admin", InitialPassword);
            }

            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(webBuilder =>
            {
                webBuilder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<LearnPipDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<LearnPipDbContext>>();
                    services.AddDbContext<LearnPipDbContext>(builder => builder.UseNpgsql(connection.ConnectionString));
                });
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false });
            var denied = await client.PostAsJsonAsync("/api/v1/auth/password", new PasswordLoginRequest("admin", InitialPassword));
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            client.DefaultRequestHeaders.Add("Origin", "https://localhost");
            var login = await client.PostAsJsonAsync("/api/v1/auth/password", new PasswordLoginRequest("ADMIN", InitialPassword));
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            var grant = (await login.Content.ReadFromJsonAsync<ApiResponse<SessionGrant>>())!.Data;
            client.DefaultRequestHeaders.Add("Cookie", $"{SessionAuthentication.CookieName}={grant.Token}");
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/admin/audit")).StatusCode);
            client.DefaultRequestHeaders.Remove("Origin");
            var csrf = await client.PostAsJsonAsync("/api/v1/auth/password/change", new PasswordChangeRequest(InitialPassword, ChangedPassword));
            Assert.Equal(HttpStatusCode.Forbidden, csrf.StatusCode);
            client.DefaultRequestHeaders.Add("Origin", "https://localhost");
            var invalid = await client.PostAsJsonAsync("/api/v1/auth/password/change", new PasswordChangeRequest("wrong", ChangedPassword));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var changed = await client.PostAsJsonAsync("/api/v1/auth/password/change", new PasswordChangeRequest(InitialPassword, ChangedPassword));
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            client.DefaultRequestHeaders.Remove("Cookie");
            var oldLogin = await client.PostAsJsonAsync("/api/v1/auth/password", new PasswordLoginRequest("admin", InitialPassword));
            Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
            var newLogin = await client.PostAsJsonAsync("/api/v1/auth/password", new PasswordLoginRequest("admin", ChangedPassword));
            Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                var credential = await db.PasswordCredentials.Include(item => item.Account).SingleAsync();
                var service = new PasswordService(db, new SessionService(db));
                await service.ResetAsync("admin", InitialPassword);
                Assert.False(await db.AccountSessions.AnyAsync(item => item.RevokedAtUtc == null));
                credential.Account.DisabledAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
                Assert.NotNull(await service.SignInAsync("admin", InitialPassword, CancellationToken.None));
                Assert.Null(credential.Account.DisabledAtUtc);
                credential.Account.DeletedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
                Assert.Null(await service.SignInAsync("admin", InitialPassword, CancellationToken.None));
                Assert.All(await db.AdministrationAuditEvents.ToListAsync(), item =>
                {
                    Assert.DoesNotContain(InitialPassword, item.NewValue ?? string.Empty, StringComparison.Ordinal);
                    Assert.DoesNotContain(ChangedPassword, item.NewValue ?? string.Empty, StringComparison.Ordinal);
                });
            }
        }
        finally
        {
            await using var admin = new NpgsqlConnection(maintenance.ConnectionString);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{name}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    /// <summary>Prüft Grenzen und Namen ohne stille Kürzung oder verpflichtende Zeichenklassen.</summary>
    [Fact]
    public void PasswordAndUsernameValidationIsExplicit()
    {
        Assert.False(PasswordService.IsValidPassword("short"));
        Assert.False(PasswordService.IsValidPassword(new string('a', 129)));
        Assert.False(PasswordService.IsValidPassword(new string(' ', 12)));
        Assert.True(PasswordService.IsValidPassword("long passphrase with spaces"));
        Assert.Equal("admin", PasswordService.NormalizeUsername(" ADMIN "));
        Assert.Null(PasswordService.NormalizeUsername("bad/name"));
    }
}
