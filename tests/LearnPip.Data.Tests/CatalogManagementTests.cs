// <copyright file="CatalogManagementTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api.CatalogPackages;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>Prüft Instanzverwaltung, bestätigte Updates und Rechtegrenzen gegen PostgreSQL.</summary>
public sealed partial class ApiV1Tests
{
    /// <summary>Ein Paketwechsel erhält Quellkennungen und alte Fassungen und schützt persönliche Entwürfe.</summary>
    /// <returns>Die ausgeführte Integrationsprüfung.</returns>
    [Fact]
    public async Task OptionalPackagesAndPrivateUpdatesRequireExplicitVersionBoundConsent()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var connection = new NpgsqlConnectionStringBuilder(source) { Database = "learnpip_packages_" + Guid.NewGuid().ToString("N") };
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
            var admin = Guid.NewGuid();
            var session = Guid.NewGuid();
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Accounts.AddRange(new Account { Id = owner }, new Account { Id = admin });
                var role = new RoleDefinition { Scope = "system", Code = "admin", Name = "Administrator" };
                db.Roles.Add(role);
                db.AccountRoles.Add(new AccountRole { AccountId = admin, RoleDefinitionId = role.Id });
                db.AccountSessions.Add(new AccountSession { Id = session, AccountId = admin, TokenHash = new string('a', 64), ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) });
                await db.SaveChangesAsync();
            }

            using var factory = ExportFactory(connection.ConnectionString);
            using var learner = ClientFor(factory, owner);
            using var administrator = ClientFor(factory, admin);
            using var stale = ClientFor(factory, admin);
            administrator.DefaultRequestHeaders.Add("X-Test-Session", session.ToString());
            var original = CatalogPackageReaderTests.Golden();
            var originalHash = Convert.ToHexStringLower(SHA256.HashData(original));
            var originalFingerprint = CatalogPackageReader.Read(original).Fingerprint;
            var changed = CatalogPackageReaderTests.Rewrite(null, "Neue synthetische Paketfrage");
            var changedHash = Convert.ToHexStringLower(SHA256.HashData(changed));
            Assert.Empty((await learner.GetFromJsonAsync<JsonElement>("/api/v1/instance-catalogs/")).GetProperty("data").EnumerateArray());
            Assert.Equal(HttpStatusCode.Forbidden, (await learner.GetAsync("/api/v1/instance-catalogs/admin/")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stale.GetAsync("/api/v1/instance-catalogs/admin/")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await InstancePackage(administrator, original, originalHash, false)).StatusCode);
            using var installed = await InstancePackage(administrator, original, originalHash, true);
            Assert.Equal(HttpStatusCode.Created, installed.StatusCode);
            var id = (await installed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.OK, (await InstancePackage(administrator, original, originalHash, true)).StatusCode);
            using (var form = new MultipartFormDataContent())
            {
                form.Add(new ByteArrayContent(changed), "file", "synthetic.zip");
                using var response = await administrator.PostAsync("/api/v1/instance-catalogs/admin/preview", form);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var changes = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
                Assert.Equal(1, changes.GetProperty("changedQuestions").GetInt32());
                Assert.Equal(0, changes.GetProperty("unchangedQuestions").GetInt32());
                Assert.Equal(0, changes.GetProperty("newQuestions").GetInt32());
                Assert.Equal(0, changes.GetProperty("removedQuestions").GetInt32());
            }

            Assert.Equal(HttpStatusCode.Conflict, (await InstancePackage(administrator, changed, changedHash, true)).StatusCode);
            Assert.Equal(original, await learner.GetByteArrayAsync($"/api/v1/instance-catalogs/{id}/archive"));
            var confirmation = new InstanceCatalogEndpoints.AvailabilityInput(false, true, originalHash);
            Assert.Equal(HttpStatusCode.BadRequest, (await administrator.PutAsJsonAsync($"/api/v1/instance-catalogs/admin/{id}/availability", confirmation with { Confirmed = false })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await administrator.PutAsJsonAsync($"/api/v1/instance-catalogs/admin/{id}/availability", confirmation)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await learner.GetAsync($"/api/v1/instance-catalogs/{id}/archive")).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await PostPackage(learner, "import", original, originalHash)).StatusCode);
            var before = (await learner.GetFromJsonAsync<JsonElement>("/api/v1/catalog-packages/")).GetProperty("data");
            Assert.Single(before.EnumerateArray());
            Assert.Equal(HttpStatusCode.Conflict, (await PostPackage(learner, "import", changed, changedHash)).StatusCode);
            using var previewResponse = await PostPackage(learner, "preview", changed);
            var preview = (await previewResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            Assert.True(preview.GetProperty("canUpdate").GetBoolean());
            Assert.Equal(originalFingerprint, preview.GetProperty("previousFingerprint").GetString());
            Assert.Equal(CatalogPackageReader.Read(original).Manifest.GetProperty("source_revision").GetString(), preview.GetProperty("previousSourceRevision").GetString());
            Assert.Equal(1, preview.GetProperty("changes").GetProperty("changedQuestions").GetInt32());
            Assert.Equal(0, preview.GetProperty("changes").GetProperty("removedQuestions").GetInt32());
            Assert.Equal(HttpStatusCode.Conflict, (await UpdatePackage(learner, changed, changedHash, new string('0', 64))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await UpdatePackage(learner, changed, changedHash, originalFingerprint)).StatusCode);
            Guid questionId;
            await using (var db = new LearnPipDbContext(options))
            {
                Assert.Equal(CatalogPackageReader.Read(original).Questions.Count, await db.Questions.CountAsync());
                Assert.Single(await db.CatalogPackageImportRevisions.ToListAsync());
                var question = await db.Questions.Include(item => item.Versions).FirstAsync();
                questionId = question.Id;
                Assert.Equal(2, question.Versions.Count);
                db.QuestionDrafts.Add(new QuestionDraft { QuestionId = questionId, PayloadJson = "{}" });
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.Conflict, (await UpdatePackage(learner, original, originalHash, originalFingerprint)).StatusCode);
            using var remove = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/instance-catalogs/admin/{id}") { Content = JsonContent.Create(confirmation) };
            Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(remove)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await InstancePackage(administrator, original, originalHash, true)).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                Assert.Empty(await db.InstanceCatalogPackages.ToListAsync());
                Assert.True(await db.QuestionDrafts.AnyAsync(item => item.QuestionId == questionId));
                Assert.Equal(2, await db.QuestionVersions.CountAsync(item => item.QuestionId == questionId));
                Assert.True(await db.AdministrationAuditEvents.AnyAsync(item => item.Action == "catalog-package-delete"));
            }
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{connection.Database}\" WITH (FORCE)", server);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<HttpResponseMessage> InstancePackage(HttpClient client, byte[] bytes, string hash, bool confirmed)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", "synthetic.zip");
        form.Add(new StringContent(hash), "archiveSha256");
        form.Add(new StringContent(confirmed ? "true" : "false"), "rightsConfirmed");
        return await client.PostAsync("/api/v1/instance-catalogs/admin/install", form);
    }

    private static async Task<HttpResponseMessage> UpdatePackage(HttpClient client, byte[] bytes, string hash, string previous)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", "synthetic.zip");
        form.Add(new StringContent(hash), "archiveSha256");
        form.Add(new StringContent("true"), "rightsConfirmed");
        form.Add(new StringContent("true"), "updateConfirmed");
        form.Add(new StringContent(previous), "previousFingerprint");
        return await client.PostAsync("/api/v1/catalog-packages/import", form);
    }
}
