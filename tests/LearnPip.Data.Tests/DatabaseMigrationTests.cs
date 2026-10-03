// <copyright file="DatabaseMigrationTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using LearnPip.Data.Domain;
using LearnPip.Data.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für die Datenbankmigrationen.
/// </summary>
public sealed class DatabaseMigrationTests
{
    /// <summary>
    /// Prüft Migrationen einer neuen Datenbank und den Erhalt bestehender Daten.
    /// </summary>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    [Fact]
    public async Task MigrationCreatesAFreshDatabaseAndPreservesExistingData()
    {
        var sourceConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip");
        if (string.IsNullOrWhiteSpace(sourceConnectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__LearnPip to a disposable local PostgreSQL server before running integration tests.");
        }

        var databaseName = $"learnpip_migration_test_{Guid.NewGuid():N}";
        var maintenanceConnectionString = new NpgsqlConnectionStringBuilder(sourceConnectionString)
        {
            Database = "postgres",
        };

        await using (var maintenanceConnection = new NpgsqlConnection(maintenanceConnectionString.ConnectionString))
        {
            await maintenanceConnection.OpenAsync();
            await using var createDatabase = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", maintenanceConnection);
            await createDatabase.ExecuteNonQueryAsync();
        }

        try
        {
            var databaseConnectionString = new NpgsqlConnectionStringBuilder(sourceConnectionString)
            {
                Database = databaseName,
            };
            var options = new DbContextOptionsBuilder<LearnPipDbContext>()
                .UseNpgsql(databaseConnectionString.ConnectionString)
                .Options;

            var ownerId = Guid.NewGuid();
            var otherOwnerId = Guid.NewGuid();
            var assetId = Guid.NewGuid();
            var deletedAssetId = Guid.NewGuid();

            await using (var context = new LearnPipDbContext(options))
            {
                await context.Database.MigrateAsync();
                context.Accounts.AddRange(new Account { Id = ownerId }, new Account { Id = otherOwnerId });
                context.MediaAssets.Add(new MediaAsset
                {
                    Id = assetId,
                    OwnerAccountId = ownerId,
                    StorageKey = $"private/{ownerId:N}/{assetId:N}",
                    MediaType = "image/jpeg",
                    ByteLength = 128,
                });
                context.MediaAssets.Add(new MediaAsset
                {
                    Id = deletedAssetId,
                    OwnerAccountId = ownerId,
                    StorageKey = $"private/{ownerId:N}/{deletedAssetId:N}",
                    MediaType = "image/jpeg",
                    ByteLength = 128,
                    DeletedAtUtc = DateTimeOffset.UtcNow,
                });
                await context.SaveChangesAsync();

                var catalog = new PrivateMediaCatalog(context);
                Assert.Single(await catalog.ListOwnedByAsync(ownerId));
                Assert.Empty(await catalog.ListOwnedByAsync(otherOwnerId));
            }

            await using (var existingContext = new LearnPipDbContext(options))
            {
                await existingContext.Database.MigrateAsync();
                Assert.True(await existingContext.Database.CanConnectAsync());
                Assert.Equal(2, await existingContext.Accounts.CountAsync());
                Assert.Equal(assetId, (await existingContext.MediaAssets.SingleAsync(asset => asset.Id == assetId)).Id);
            }
        }
        finally
        {
            await using var maintenanceConnection = new NpgsqlConnection(maintenanceConnectionString.ConnectionString);
            await maintenanceConnection.OpenAsync();
            await using (var terminateConnections = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid()",
                maintenanceConnection))
            {
                terminateConnections.Parameters.AddWithValue("databaseName", databaseName);
                await terminateConnections.ExecuteNonQueryAsync();
            }

            await using var dropDatabase = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", maintenanceConnection);
            await dropDatabase.ExecuteNonQueryAsync();
        }
    }
}
