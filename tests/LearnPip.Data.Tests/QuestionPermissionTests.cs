// <copyright file="QuestionPermissionTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using LearnPip.Api;
using LearnPip.Api.Administration;
using LearnPip.Api.Identity;
using LearnPip.Api.Questions;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>Prüft aktuelle Rollen-/Objektrechte über echte HTTP-Endpunkte und PostgreSQL.</summary>
public sealed class QuestionPermissionTests
{
    /// <summary>Prüft Verweigerung bei fehlender, unbekannter oder beschädigter Matrix.</summary>
    /// <param name="value">Ungültige gespeicherte Konfiguration.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("{bad")]
    [InlineData("{\"editForeign\":true}")]
    [InlineData("{\"unknown\":true}")]
    public void InvalidConfigurationDenies(string? value) => Assert.Empty(QuestionPermissions.Parse(value));

    /// <summary>Prüft Eigentümer, fremde/private/öffentliche/Gruppenfragen, sofortigen Entzug, Medien, Export und Audit.</summary>
    /// <returns>Asynchrone Testoperation.</returns>
    [Fact]
    public async Task RightsAreEnforcedAcrossHttpObjectsAndActiveSessions()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var name = $"learnpip_permissions_{Guid.NewGuid():N}";
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        var database = new NpgsqlConnectionStringBuilder(source) { Database = name };
        await using (var connection = new NpgsqlConnection(maintenance.ConnectionString))
        {
            await connection.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<LearnPipDbContext>().UseNpgsql(database.ConnectionString).Options;
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                Assert.Equal(32, await db.SystemSettings.CountAsync(setting => setting.Key.StartsWith("questions.permissions.")));
            }

            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<LearnPipDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<LearnPipDbContext>>();
                    services.AddDbContext<LearnPipDbContext>(db => db.UseNpgsql(database.ConnectionString));
                }));
            using var anonymous = factory.CreateClient();
            var admin = await Account(anonymous);
            var moderator = await Account(anonymous);
            var owner = await Account(anonymous);
            var other = await Account(anonymous);
            using var adminClient = Client(factory, admin.Session.Token);
            using var moderatorClient = Client(factory, moderator.Session.Token);
            using var ownerClient = Client(factory, owner.Session.Token);
            using var otherClient = Client(factory, other.Session.Token);
            await using (var db = new LearnPipDbContext(options))
            {
                var adminRole = new RoleDefinition { Scope = "system", Code = "admin", Name = "Admin" };
                var moderatorRole = new RoleDefinition { Scope = "system", Code = "moderator", Name = "Moderator" };
                db.AccountRoles.AddRange(
                    new AccountRole { AccountId = admin.AccountId, RoleDefinition = adminRole },
                    new AccountRole { AccountId = moderator.AccountId, RoleDefinition = moderatorRole });
                await db.SaveChangesAsync();
            }

            var ids = new List<Guid>();
            var versions = new List<Guid>();
            var mediaId = Guid.NewGuid();
            await using (var db = new LearnPipDbContext(options))
            {
                db.MediaAssets.Add(new MediaAsset { Id = mediaId, OwnerAccountId = owner.AccountId, MediaType = "image/png", AltText = "Testbild", ByteLength = 3 });
                db.MediaBlobs.Add(new MediaBlob { MediaAssetId = mediaId, Data = new byte[] { 1, 2, 3 } });
                await db.SaveChangesAsync();
            }

            foreach (var visibility in new[] { "private", "group", "public" })
            {
                var response = await ownerClient.PostAsJsonAsync("/api/v1/questions/", Content("Originalfrage", mediaId));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                var published = (await response.Content.ReadFromJsonAsync<ApiResponse<PublishedQuestionVersion>>())!.Data;
                Guid id;
                await using (var db = new LearnPipDbContext(options))
                {
                    var version = await db.QuestionVersions.SingleAsync(item => item.Id == published.Id);
                    id = version.QuestionId;
                    version.Visibility = visibility == "public" ? "public" : "private";
                    version.AuthorAttribution = "Originalautor";
                    if (visibility == "public")
                    {
                        db.PublicSubmissions.Add(new PublicSubmission { QuestionVersionId = version.Id, AccountId = owner.AccountId, Status = "approved", LicenseChoice = "CC BY-SA 4.0", AuthorAttribution = "Originalautor" });
                    }

                    if (visibility == "group")
                    {
                        var group = new StudyGroup { OwnerAccountId = other.AccountId, Name = "Testgruppe" };
                        db.GroupQuestionShares.Add(new GroupQuestionShare { QuestionId = id, StudyGroup = group, SharedByAccountId = owner.AccountId });
                    }

                    await db.SaveChangesAsync();
                }

                ids.Add(id);
                versions.Add(published.Id);
                Assert.Equal(HttpStatusCode.NotFound, (await otherClient.PostAsJsonAsync($"/api/v1/questions/{id}/versions", Content("Angriff", null))).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.PostAsJsonAsync("/api/v1/questions/delete", new QuestionDeleteInput(new[] { id }, "Unberechtigte Löschung", true))).StatusCode);
                Assert.Equal(
                    visibility == "public" ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                    (await moderatorClient.GetAsync($"/api/v1/questions/{id}/versions/1")).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{published.Id}/inspect", new ModerationInspectInput("Korrektheit der Frage prüfen"))).StatusCode);
                var corrected = await moderatorClient.PostAsJsonAsync(
                    $"/api/v1/moderation/questions/{id}/revise",
                    new QuestionRevisionInput(Content("Korrigierte Frage", mediaId), "Fachlichen Fehler korrigieren", 1));
                Assert.Equal(HttpStatusCode.Created, corrected.StatusCode);
                var stale = await moderatorClient.PostAsJsonAsync(
                    $"/api/v1/moderation/questions/{id}/revise",
                    new QuestionRevisionInput(Content("Veraltete Änderung", mediaId), "Fachlichen Fehler korrigieren", 1));
                Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
                await using (var db = new LearnPipDbContext(options))
                {
                    var history = await db.QuestionVersions.Where(version => version.QuestionId == id).OrderBy(version => version.VersionNumber).ToListAsync();
                    Assert.Equal("Originalfrage [Bild]", history[0].Prompt);
                    Assert.Equal("private", history[1].Visibility);
                    Assert.Equal("Originalautor", history[1].AuthorAttribution);
                    Assert.Equal(moderator.AccountId, history[1].CreatedByAccountId);
                    Assert.True(await db.QuestionModerationEvents.AnyAsync(item => item.QuestionVersionId == history[0].Id && item.ReplacementVersionId == history[1].Id));
                }
            }

            var draftResponse = await ownerClient.PostAsJsonAsync("/api/v1/questions/drafts", new DraftSaveRequest(Content("Privater Entwurf", mediaId), null));
            Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
            var draft = (await draftResponse.Content.ReadFromJsonAsync<ApiResponse<DraftView>>())!.Data;
            Assert.Equal(HttpStatusCode.OK, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{draft.QuestionId}/inspect", new ModerationInspectInput("Entwurf auf Korrektheit prüfen"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{draft.QuestionId}/media/{mediaId}", new ModerationInspectInput("Entwurfsbild auf Korrektheit prüfen"))).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{draft.QuestionId}/revise", new QuestionRevisionInput(Content("Geprüfter Entwurf", mediaId), "Entwurf vollständig korrigieren", 0))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync($"/api/v1/questions/{draft.QuestionId}/draft")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.PostAsJsonAsync("/api/v1/questions/delete", new QuestionDeleteInput(new[] { draft.QuestionId }, "Fremde Frage entfernen", true))).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{versions[0]}/media/{mediaId}", new ModerationInspectInput("Abgebildeten Inhalt prüfen"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{versions[0]}/media/{Guid.NewGuid()}", new ModerationInspectInput("Abgebildeten Inhalt prüfen"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await moderatorClient.GetAsync("/api/v1/admin/question-permissions/")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await moderatorClient.PutAsJsonAsync("/api/v1/admin/question-permissions/user", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.PostAsJsonAsync("/api/v1/moderation/questions/browse", new ModerationBrowseInput("Korrektheit der Frage prüfen"))).StatusCode);
            var candidates = await moderatorClient.GetFromJsonAsync<ApiResponse<JsonElement>>("/api/v1/catalog-exports/questions");
            Assert.Empty(candidates!.Data.EnumerateArray());

            var foreignExport = new
            {
                questionIds = ids,
                title = "Unberechtigter Export",
                publisher = "Test",
                questionLicense = new { id = "CC BY-SA 4.0", holder = "Test", attribution = "Test" },
                imageLicense = new { id = "CC BY-SA 4.0", holder = "Test", attribution = "Test" },
                licenseNotice = "Test",
                rightsConfirmed = true,
            };
            Assert.Equal(HttpStatusCode.NotFound, (await moderatorClient.PostAsJsonAsync("/api/v1/catalog-exports/preview", foreignExport)).StatusCode);

            var started = await ownerClient.PostAsJsonAsync("/api/v1/learning/sessions/", new StartLearningRequest(null, 1, "de"));
            Assert.Equal(HttpStatusCode.Created, started.StatusCode);
            var session = (await started.Content.ReadFromJsonAsync<ApiResponse<LearningSessionView>>())!.Data;

            var archive = await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "catalog-golden.zip"));
            using var import = new MultipartFormDataContent();
            import.Add(new ByteArrayContent(archive), "file", "catalog.zip");
            import.Add(new StringContent(Convert.ToHexStringLower(SHA256.HashData(archive))), "archiveSha256");
            import.Add(new StringContent("true"), "rightsConfirmed");
            Assert.Equal(HttpStatusCode.Created, (await ownerClient.PostAsync("/api/v1/catalog-packages/import", import)).StatusCode);
            QuestionVersion imported;
            await using (var db = new LearnPipDbContext(options))
            {
                var package = await db.CatalogPackageImports.SingleAsync();
                var importedId = JsonSerializer.Deserialize<Dictionary<string, Guid>>(package.QuestionIdsJson)!.Values.First();
                imported = await db.QuestionVersions.AsNoTracking().SingleAsync(version => version.QuestionId == importedId);
            }

            var relabeled = Content("Bearbeitete Importfrage", null) with { Source = "Andere Herkunft", License = "CC0-1.0" };
            Assert.Equal(HttpStatusCode.Created, (await ownerClient.PostAsJsonAsync($"/api/v1/questions/{imported.QuestionId}/versions", relabeled)).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                var latest = await db.QuestionVersions.AsNoTracking().Where(version => version.QuestionId == imported.QuestionId).OrderByDescending(version => version.VersionNumber).FirstAsync();
                Assert.Equal(imported.Source, latest.Source);
                Assert.Equal(imported.License, latest.License);
                Assert.Equal(imported.AuthorAttribution, latest.AuthorAttribution);
                Assert.Equal(archive, await db.CatalogPackageImports.Select(package => package.Archive).SingleAsync());
            }

            var originalUser = await Value(options, "user");
            var rights = QuestionPermissions.Defaults("user");
            rights["readForeign"] = rights["readPrivate"] = rights["editForeign"] = true;
            Assert.Equal(HttpStatusCode.BadRequest, (await Save(adminClient, "user", rights, originalUser, false, false)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await Save(adminClient, "user", rights, originalUser, true, false)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await Save(adminClient, "user", rights, originalUser, true, true)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await otherClient.PostAsJsonAsync($"/api/v1/moderation/questions/{versions[0]}/inspect", new ModerationInspectInput("Korrektheit der Frage prüfen"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await otherClient.GetAsync("/api/v1/admin/audit")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await Save(adminClient, "user", QuestionPermissions.Defaults("user"), originalUser, true, true)).StatusCode);
            var currentUser = await Value(options, "user");
            rights = QuestionPermissions.Defaults("user");
            rights["editOwn"] = rights["deleteOwn"] = rights["create"] = rights["readOwn"] = rights["readShared"] = rights["import"] = rights["export"] = rights["community"] = false;
            Assert.Equal(HttpStatusCode.NoContent, (await Save(adminClient, "user", rights, currentUser, true, true)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsJsonAsync($"/api/v1/questions/{ids[0]}/versions", Content("Gesperrt", null))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsJsonAsync("/api/v1/questions/drafts", new DraftSaveRequest(Content("Gesperrt", null), null))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/questions/{ids[0]}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/media/{mediaId}/content")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.GetAsync("/api/v1/catalog-exports/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.GetAsync($"/api/v1/learning/sessions/{session.Id}")).StatusCode);
            using var blockedImport = new MultipartFormDataContent();
            blockedImport.Add(new ByteArrayContent(new byte[] { 1 }), "file", "test.zip");
            blockedImport.Add(new StringContent("untrusted"), "archiveSha256");
            blockedImport.Add(new StringContent("true"), "rightsConfirmed");
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsync("/api/v1/catalog-packages/import", blockedImport)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await ownerClient.PostAsJsonAsync("/api/v1/questions/delete", new QuestionDeleteInput(new[] { ids[0] }, "Eigene Frage entfernen", true))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/api/v1/admin/question-permissions/")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await adminClient.DeleteAsync($"/api/v1/admin/accounts/{admin.AccountId}/roles/admin")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await Save(adminClient, "user", QuestionPermissions.Defaults("user"), await Value(options, "user"), true, true)).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await ownerClient.PostAsJsonAsync($"/api/v1/questions/{ids[0]}/versions", Content("Wieder erlaubt", mediaId))).StatusCode);

            var modRights = QuestionPermissions.Defaults("moderator");
            modRights["deleteForeign"] = false;
            Assert.Equal(HttpStatusCode.NoContent, (await Save(adminClient, "moderator", modRights, await Value(options, "moderator"), true, true)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await moderatorClient.PostAsJsonAsync("/api/v1/moderation/questions/delete", new QuestionDeleteInput(ids, "Mehrere Fragen entfernen", true))).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                Assert.False(await db.Questions.AnyAsync(question => ids.Contains(question.Id) && question.DeletedAtUtc != null));
                Assert.True(await db.AdministrationAuditEvents.AnyAsync(item => item.Action == "questions.permissions.changed" && item.ActorAccountId == admin.AccountId && item.PreviousValue != null));
                var grant = await db.AccountRoles.SingleAsync(item => item.AccountId == moderator.AccountId);
                db.AccountRoles.Remove(grant);
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.Forbidden, (await moderatorClient.PostAsJsonAsync($"/api/v1/moderation/questions/{versions[0]}/inspect", new ModerationInspectInput("Korrektheit der Frage prüfen"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await adminClient.PutAsync($"/api/v1/admin/accounts/{moderator.AccountId}/roles/moderator", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await adminClient.PutAsync($"/api/v1/admin/accounts/{moderator.AccountId}/roles/moderator?privacyConfirmed=true", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await adminClient.DeleteAsync($"/api/v1/admin/accounts/{moderator.AccountId}/roles/moderator")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await adminClient.PostAsJsonAsync("/api/v1/moderation/questions/delete", new QuestionDeleteInput(ids, "Mehrere Fragen entfernen", false))).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                var read = await db.SystemSettings.SingleAsync(item => item.Key == "questions.permissions.user.readOwn");
                read.Value = "invalid";
                await db.SaveChangesAsync();
                Assert.False(await QuestionPermissions.Allows(db, owner.AccountId, "readOwn", CancellationToken.None));
                Assert.False(await QuestionPermissions.Allows(db, admin.AccountId, "unknown", CancellationToken.None));
            }

            Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/questions/{draft.QuestionId}/versions/1")).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                var read = await db.SystemSettings.SingleAsync(item => item.Key == "questions.permissions.user.readOwn");
                db.SystemSettings.Remove(read);
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/questions/{draft.QuestionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await Save(adminClient, "user", QuestionPermissions.Defaults("user"), await Value(options, "user"), true, true)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await adminClient.PostAsJsonAsync("/api/v1/moderation/questions/delete", new QuestionDeleteInput(ids, "Mehrere Fragen entfernen", true))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await adminClient.PostAsJsonAsync("/api/v1/moderation/questions/delete", new QuestionDeleteInput(new[] { draft.QuestionId }, "Entwurf vollständig entfernen", true))).StatusCode);
            var afterDeletion = await ownerClient.GetFromJsonAsync<ApiResponse<LearningSessionView>>($"/api/v1/learning/sessions/{session.Id}");
            Assert.Null(afterDeletion!.Data.Current);
            Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/v1/questions/{ids[0]}/versions/1")).StatusCode);
        }
        finally
        {
            await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
            await connection.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static QuestionPublishRequest Content(string prompt, Guid? image) => new(
        "single",
        "Technik",
        "Prüfung",
        "de",
        "Originalquelle",
        "CC BY-SA 4.0",
        image.HasValue ? new[] { new ContentBlockInput("text", prompt, null), new ContentBlockInput("image", null, image) } : new[] { new ContentBlockInput("text", prompt, null) },
        Array.Empty<ContentBlockInput>(),
        new[] { new AnswerInput(true, new[] { new ContentBlockInput("text", "Richtig", null) }), new AnswerInput(false, new[] { new ContentBlockInput("text", "Falsch", null) }) });

    private static async Task<string> Value(DbContextOptions<LearnPipDbContext> options, string role)
    {
        await using var db = new LearnPipDbContext(options);
        return JsonSerializer.Serialize(await QuestionPermissions.Snapshot(db, role, CancellationToken.None));
    }

    private static Task<HttpResponseMessage> Save(HttpClient client, string role, Dictionary<string, bool> rights, string value, bool foreign, bool privacy) =>
        client.PutAsJsonAsync("/api/v1/admin/question-permissions/" + role, new QuestionPermissionInput(rights, value, "Regressionstest", foreign, privacy));

    private static async Task<NewAccount> Account(HttpClient client) =>
        (await (await client.PostAsJsonAsync("/api/v1/auth/pseudonymous", new { })).Content.ReadFromJsonAsync<ApiResponse<NewAccount>>())!.Data;

    private static HttpClient Client(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
