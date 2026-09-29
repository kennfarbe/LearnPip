using LearnPip.Api.Identity;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace LearnPip.Data.Tests;

public sealed class AccountLifecycleTests
{
    [Fact]
    public async Task Lifecycle_warns_once_per_phase_rechecks_activity_and_deletes_private_data()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var name = $"learnpip_lifecycle_test_{Guid.NewGuid():N}";
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        var database = new NpgsqlConnectionStringBuilder(source) { Database = name };
        await using (var admin = new NpgsqlConnection(maintenance.ConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<LearnPipDbContext>()
                .UseNpgsql(database.ConnectionString).Options;
            var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var lastActivity = now.AddDays(-60);
            var accountId = Guid.NewGuid();
            var moderatorId = Guid.NewGuid();
            var recoverableId = Guid.NewGuid();
            var secret = SessionAuthentication.NewSecret();
            var mediaId = Guid.NewGuid();
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                var moderator = new Account { Id = moderatorId, LastActivityAtUtc = lastActivity.AddDays(-200) };
                var role = new RoleDefinition { Scope = "system", Code = "moderator", Name = "Moderator" };
                db.Accounts.AddRange(new Account { Id = accountId, LastActivityAtUtc = lastActivity }, moderator,
                    new Account { Id = recoverableId, LastActivityAtUtc = lastActivity.AddDays(-30) });
                db.RecoveryCredentials.Add(new RecoveryCredential
                {
                    AccountId = recoverableId, SecretHash = SessionAuthentication.Hash(secret)
                });
                db.Roles.Add(role);
                db.AccountRoles.Add(new AccountRole { AccountId = moderatorId, RoleDefinition = role });
                db.ExternalIdentities.Add(new ExternalIdentity
                {
                    AccountId = accountId, Provider = "email", Subject = "old@example.org"
                });
                db.Questions.Add(new Question { OwnerAccountId = accountId });
                db.MediaAssets.Add(new MediaAsset
                {
                    Id = mediaId, OwnerAccountId = accountId, StorageKey = $"private/{accountId:N}/{mediaId:N}",
                    MediaType = "image/png", ByteLength = 3
                });
                db.MediaBlobs.Add(new MediaBlob { MediaAssetId = mediaId, Data = [1, 2, 3] });
                await db.SaveChangesAsync();
            }

            var sender = new RecordingSender();
            await using (var db = new LearnPipDbContext(options))
            {
                var service = new AccountLifecycleService(db, sender);
                Assert.Equal(1, (await service.RunOnceAsync(now)).WarningsClaimed);
                Assert.Equal(0, (await service.RunOnceAsync(now)).WarningsClaimed);
                Assert.Equal(1, (await service.RunOnceAsync(now.AddDays(16))).WarningsClaimed);
                Assert.Equal(1, (await service.RunOnceAsync(now.AddDays(27))).WarningsClaimed);
                Assert.Equal([60, 76, 87], sender.Phases);
                Assert.Equal(1, (await service.RunOnceAsync(now.AddDays(30))).Deactivated);
                Assert.NotNull((await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == accountId)).DisabledAtUtc);
                Assert.NotNull((await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == recoverableId)).DisabledAtUtc);
                var identity = new IdentityService(db, new NoMailSender(), new ConfigurationBuilder().Build());
                Assert.Equal(recoverableId, await identity.RecoverAsync(secret, CancellationToken.None));
                Assert.Null((await db.Accounts.AsNoTracking().SingleAsync(x => x.Id == recoverableId)).DisabledAtUtc);
                Assert.Equal(0, (await service.RunOnceAsync(now.AddDays(119))).Deleted);
                Assert.Equal(1, (await service.RunOnceAsync(now.AddDays(120))).Deleted);
                Assert.False(await db.Accounts.AnyAsync(x => x.Id == accountId));
                Assert.False(await db.MediaBlobs.AnyAsync(x => x.MediaAssetId == mediaId));
                Assert.False(await db.Questions.AnyAsync(x => x.OwnerAccountId == accountId));
                Assert.True(await db.Accounts.AnyAsync(x => x.Id == moderatorId));
                Assert.True(await db.Accounts.AnyAsync(x => x.Id == recoverableId));
            }
        }
        finally
        {
            await using var admin = new NpgsqlConnection(maintenance.ConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @name AND pid <> pg_backend_pid()", admin))
            {
                terminate.Parameters.AddWithValue("name", name);
                await terminate.ExecuteNonQueryAsync();
            }
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class RecordingSender : IInactivityNoticeSender
    {
        public bool IsAvailable => true;
        public List<int> Phases { get; } = [];
        public Task SendAsync(string email, int phaseDays, DateTimeOffset lastActivityAtUtc,
            CancellationToken cancellationToken)
        {
            Phases.Add(phaseDays);
            return Task.CompletedTask;
        }
    }

    private sealed class NoMailSender : IEmailCodeSender
    {
        public bool IsAvailable => false;
        public Task SendAsync(string email, string code, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
