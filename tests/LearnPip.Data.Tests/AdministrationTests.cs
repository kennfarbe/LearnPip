using System.Security.Claims;
using LearnPip.Api.Administration;
using LearnPip.Api.Identity;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LearnPip.Data.Tests;

public sealed class AdministrationTests
{
    [Fact]
    public async Task Bootstrap_roles_and_group_leadership_are_scoped_and_audited()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var name = $"learnpip_admin_test_{Guid.NewGuid():N}";
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
            var options = new DbContextOptionsBuilder<LearnPipDbContext>()
                .UseNpgsql(connection.ConnectionString).Options;
            var first = Guid.NewGuid();
            var leader = Guid.NewGuid();
            var target = Guid.NewGuid();
            var group = Guid.NewGuid();
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Accounts.AddRange(new Account { Id = first }, new Account { Id = leader },
                    new Account { Id = target });
                db.StudyGroups.Add(new StudyGroup { Id = group, OwnerAccountId = first, Name = "Study" });
                await db.SaveChangesAsync();
                var service = new AdministrationService(db);
                await service.BootstrapAsync(first);
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.BootstrapAsync(leader));
                Assert.Equal("last_admin", await service.SetSystemRoleAsync(first, first, "admin", false,
                    CancellationToken.None));
                Assert.Equal("changed", await service.SetGroupRoleAsync(first, group, leader, "leader",
                    CancellationToken.None));
                Assert.Equal("changed", await service.SetGroupRoleAsync(leader, group, target, "member",
                    CancellationToken.None));
                Assert.Equal("changed", await service.SetSystemRoleAsync(first, leader, "moderator", true,
                    CancellationToken.None));
                Assert.Equal("changed", await service.SetMaintenanceNoticeAsync(first, "planned downtime",
                    CancellationToken.None));
                Assert.Equal("forbidden", await service.SetGroupRoleAsync(target, group, first, "member",
                    CancellationToken.None));
                Assert.Equal(5, await db.AdministrationAuditEvents.CountAsync());
                Assert.False(await db.AccountRoles.AnyAsync(item => item.AccountId == target));
                Assert.True(await db.GroupMemberships.AnyAsync(item => item.StudyGroupId == group &&
                    item.AccountId == target && item.RoleDefinition.Scope == "group" &&
                    item.RoleDefinition.Code == "member"));
            }

            await using (var db = new LearnPipDbContext(options))
            {
                var sessions = new SessionService(db);
                await sessions.CreateAsync(first);
                var session = await db.AccountSessions.SingleAsync();
                var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(AccountIdentity.AccountIdClaim, first.ToString()),
                    new Claim(SessionAuthentication.SessionIdClaim, session.Id.ToString())
                ], SessionAuthentication.Scheme));
                var services = new ServiceCollection();
                services.AddLogging();
                services.AddSingleton(db);
                services.AddApiAuthorization();
                await using var provider = services.BuildServiceProvider();
                var authorization = provider.GetRequiredService<IAuthorizationService>();
                Assert.True((await authorization.AuthorizeAsync(principal, null, ApiPolicies.Admin)).Succeeded);
                session.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-16);
                await db.SaveChangesAsync();
                Assert.False((await authorization.AuthorizeAsync(principal, null, ApiPolicies.Admin)).Succeeded);
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
}
