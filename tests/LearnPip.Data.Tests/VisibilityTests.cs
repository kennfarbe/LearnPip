// <copyright file="VisibilityTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LearnPip.Api;
using LearnPip.Api.CatalogPackages;
using LearnPip.Api.Groups;
using LearnPip.Api.Identity;
using LearnPip.Api.Questions;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>
/// Enthält Regressionstests für die Inhaltsfreigaben.
/// </summary>
public sealed class VisibilityTests
{
    /// <summary>
    /// Prüft die Sichtbarkeit ausdrücklich freigegebener Fragenfassungen und referenzierter Medien.
    /// </summary>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    [Fact]
    public async Task OnlyExplicitVersionsAndTheirReferencedMediaAreVisible()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var name = $"learnpip_visibility_test_{Guid.NewGuid():N}";
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
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
            }

            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.UseSetting("CatalogPackages:CommunityExportEnabled", "true").ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<LearnPipDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<LearnPipDbContext>>();
                    services.AddDbContext<LearnPipDbContext>(dbOptions =>
                        dbOptions.UseNpgsql(database.ConnectionString));
                }));
            using var anonymous = factory.CreateClient();
            var owner = await CreateAccount(anonymous);
            var member = await CreateAccount(anonymous);
            var leader = await CreateAccount(anonymous);
            var outsider = await CreateAccount(anonymous);
            var moderator = await CreateAccount(anonymous);
            var admin = await CreateAccount(anonymous);
            using var ownerClient = Client(factory, owner.Session.Token);
            using var memberClient = Client(factory, member.Session.Token);
            using var leaderClient = Client(factory, leader.Session.Token);
            using var outsiderClient = Client(factory, outsider.Session.Token);
            using var moderatorClient = Client(factory, moderator.Session.Token);
            using var adminClient = Client(factory, admin.Session.Token);

            var catalogResponse = await ownerClient.PostAsJsonAsync(
                "/api/v1/catalogs/",
                new { name = "Schule" });
            Assert.Equal(HttpStatusCode.Created, catalogResponse.StatusCode);
            var catalog = (await catalogResponse.Content.ReadFromJsonAsync<ApiResponse<IdView>>())!.Data;
            var groupResponse = await ownerClient.PostAsJsonAsync(
                "/api/v1/groups/",
                new GroupInput("Klasse"));
            var group = (await groupResponse.Content.ReadFromJsonAsync<ApiResponse<GroupView>>())!.Data;
            var questionId = Guid.NewGuid();
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var imageId = Guid.NewGuid();
            var unrelatedImageId = Guid.NewGuid();
            await using (var db = new LearnPipDbContext(options))
            {
                var memberRole = new RoleDefinition { Scope = "group", Code = "member", Name = "Member" };
                var leaderRole = new RoleDefinition { Scope = "group", Code = "leader", Name = "Leader" };
                var moderatorRole = new RoleDefinition { Scope = "system", Code = "moderator", Name = "Moderator" };
                var adminRole = new RoleDefinition { Scope = "system", Code = "admin", Name = "Admin" };
                db.Roles.AddRange(memberRole, leaderRole, moderatorRole, adminRole);
                db.GroupMemberships.AddRange(
                    new GroupMembership
                    {
                        StudyGroupId = group.Id,
                        AccountId = member.AccountId,
                        RoleDefinition = memberRole,
                    },
                    new GroupMembership
                    {
                        StudyGroupId = group.Id,
                        AccountId = leader.AccountId,
                        RoleDefinition = leaderRole,
                    });
                db.AccountRoles.AddRange(
                    new AccountRole { AccountId = moderator.AccountId, RoleDefinition = moderatorRole },
                    new AccountRole { AccountId = admin.AccountId, RoleDefinition = adminRole });
                db.Questions.Add(new Question
                {
                    Id = questionId,
                    OwnerAccountId = owner.AccountId,
                    PrivateCatalogId = catalog.Id,
                });
                db.QuestionVersions.Add(new QuestionVersion
                {
                    Id = firstId,
                    QuestionId = questionId,
                    CreatedByAccountId = owner.AccountId,
                    VersionNumber = 1,
                    Prompt = "Erste Fassung",
                    Source = "Eigener Text",
                });
                foreach (var image in new[] { imageId, unrelatedImageId })
                {
                    db.MediaAssets.Add(new MediaAsset
                    {
                        Id = image,
                        OwnerAccountId = owner.AccountId,
                        StorageKey = $"private/{image:N}",
                        MediaType = "image/png",
                        ByteLength = 3,
                    });
                    db.MediaBlobs.Add(new MediaBlob { MediaAssetId = image, Data = [1, 2, 3] });
                }

                db.QuestionContentBlocks.Add(new QuestionContentBlock
                {
                    QuestionVersionId = firstId,
                    Section = "prompt",
                    SortOrder = 0,
                    Kind = "image",
                    MediaAssetId = imageId,
                });
                await db.SaveChangesAsync();
            }

            var expectedResult13 = HttpStatusCode.NotFound;
            var actualResult14 = (await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/1")).StatusCode;

            Assert.Equal(
                expectedResult13,
                actualResult14);
            var expectedResult15 = HttpStatusCode.NotFound;
            var actualResult16 = (await outsiderClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult15,
                actualResult16);
            var expectedResult17 = HttpStatusCode.NotFound;
            var actualResult18 = (await moderatorClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult17,
                actualResult18);
            var expectedResult19 = HttpStatusCode.NotFound;
            var actualResult20 = (await adminClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult19,
                actualResult20);
            var moderationReason = "Pruefung einer privaten Frage im Moderationskontext";
            var unauthorizedBrowse = await outsiderClient.PostAsJsonAsync(
                "/api/v1/moderation/questions/browse",
                new { reason = moderationReason, page = 0 });
            Assert.Equal(HttpStatusCode.Forbidden, unauthorizedBrowse.StatusCode);

            var invalidBrowse = await moderatorClient.PostAsJsonAsync(
                "/api/v1/moderation/questions/browse",
                new { reason = "kurz", page = 0 });
            Assert.Equal(HttpStatusCode.BadRequest, invalidBrowse.StatusCode);

            var browse = await moderatorClient.PostAsJsonAsync(
                "/api/v1/moderation/questions/browse",
                new { reason = moderationReason, page = 0 });
            Assert.Equal(HttpStatusCode.OK, browse.StatusCode);
            var inspectUnauthorized = await outsiderClient.PostAsJsonAsync(
                $"/api/v1/moderation/questions/{firstId}/inspect",
                new { reason = moderationReason });
            Assert.Equal(HttpStatusCode.Forbidden, inspectUnauthorized.StatusCode);

            var inspected = await moderatorClient.PostAsJsonAsync(
                $"/api/v1/moderation/questions/{firstId}/inspect",
                new { reason = moderationReason });
            Assert.Equal(HttpStatusCode.OK, inspected.StatusCode);
            var adminInspected = await adminClient.PostAsJsonAsync(
                $"/api/v1/moderation/questions/{firstId}/inspect",
                new { reason = moderationReason });
            Assert.Equal(HttpStatusCode.OK, adminInspected.StatusCode);
            await using (var audit = new LearnPipDbContext(options))
            {
                Assert.True(await audit.AdministrationAuditEvents.AnyAsync(item =>
                    item.ActorAccountId == moderator.AccountId &&
                    item.Action == "moderation.questions.browse"));
                Assert.True(await audit.QuestionModerationEvents.AnyAsync(item =>
                    item.QuestionVersionId == firstId &&
                    item.ModeratorAccountId == moderator.AccountId &&
                    item.Action == "inspect"));
            }

            var expectedResult21 = HttpStatusCode.OK;
            var actualResult22 = (await ownerClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult21,
                actualResult22);
            var expectedResult23 = HttpStatusCode.NoContent;
            var actualResult24 = (await ownerClient.PutAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}",
                null)).StatusCode;
            Assert.Equal(
                expectedResult23,
                actualResult24);

            await using (var db = new LearnPipDbContext(options))
            {
                db.QuestionVersions.Add(new QuestionVersion
                {
                    Id = secondId,
                    QuestionId = questionId,
                    CreatedByAccountId = owner.AccountId,
                    VersionNumber = 2,
                    Prompt = "Neue private Fassung",
                    Source = "Eigener Text",
                });
                await db.SaveChangesAsync();
            }

            foreach (var client in new[] { memberClient, leaderClient })
            {
                var groupPage = (await (await client.GetAsync(
                    $"/api/v1/groups/{group.Id}/questions")).Content
                    .ReadFromJsonAsync<ApiResponse<PageResponse<QuestionSummary>>>())!.Data;
                Assert.Single(groupPage.Items);
                Assert.Equal(1, groupPage.Items[0].Version);
                var expectedResult1 = HttpStatusCode.OK;
                var actualResult2 = (await client.GetAsync(
                    $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
                Assert.Equal(
                    expectedResult1,
                    actualResult2);
                var expectedResult3 = HttpStatusCode.NotFound;
                var actualResult4 = (await client.GetAsync(
                    $"/api/v1/questions/{questionId}/versions/2")).StatusCode;
                Assert.Equal(
                    expectedResult3,
                    actualResult4);
                var expectedResult5 = HttpStatusCode.NotFound;
                var actualResult6 = (await client.PostAsJsonAsync(
                    $"/api/v1/questions/{questionId}/attempts",
                    new GradeRequest(secondId, []))).StatusCode;
                Assert.Equal(
                    expectedResult5,
                    actualResult6);
                var expectedResult7 = HttpStatusCode.OK;
                var actualResult8 = (await client.GetAsync(
                    $"/api/v1/media/{imageId}/content")).StatusCode;
                Assert.Equal(
                    expectedResult7,
                    actualResult8);
                var expectedResult9 = HttpStatusCode.NotFound;
                var actualResult10 = (await client.GetAsync(
                    $"/api/v1/media/{unrelatedImageId}/content")).StatusCode;
                Assert.Equal(
                    expectedResult9,
                    actualResult10);
            }

            foreach (var client in new[] { outsiderClient, moderatorClient, adminClient })
            {
                var expectedResult11 = HttpStatusCode.NotFound;
                var actualResult12 = (await client.GetAsync(
                                    $"/api/v1/media/{imageId}/content")).StatusCode;
                Assert.Equal(
                                    expectedResult11,
                                    actualResult12);
            }

            var expectedResult25 = HttpStatusCode.NoContent;
            var actualResult26 = (await ownerClient.PutAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}",
                null)).StatusCode;

            Assert.Equal(
                expectedResult25,
                actualResult26);
            var expectedResult27 = HttpStatusCode.OK;
            var actualResult28 = (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/2")).StatusCode;
            Assert.Equal(
                expectedResult27,
                actualResult28);
            var expectedResult29 = HttpStatusCode.NotFound;
            var actualResult30 = (await leaderClient.PutAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/visibility",
                new VersionVisibilityInput("public"))).StatusCode;
            Assert.Equal(
                expectedResult29,
                actualResult30);
            var expectedResult31 = HttpStatusCode.Conflict;
            var actualResult32 = (await ownerClient.PutAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/visibility",
                new VersionVisibilityInput("public"))).StatusCode;
            Assert.Equal(
                expectedResult31,
                actualResult32);
            var expectedResult33 = HttpStatusCode.BadRequest;
            var actualResult34 = (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission",
                new PublicSubmissionInput(
                    "invalid",
                    "CC BY 4.0",
                    "Eigener Name",
                    true,
                    true,
                    "adult"))).StatusCode;
            Assert.Equal(
                expectedResult33,
                actualResult34);
            var previewResponse = await ownerClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission-preview");
            Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
            var preview = (await previewResponse.Content.ReadFromJsonAsync<ApiResponse<PublicPreview>>())!.Data;
            Assert.True(preview.HasImages);
            var expectedResult35 = HttpStatusCode.BadRequest;
            var actualResult36 = (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission",
                new PublicSubmissionInput(
                    preview.PreviewToken,
                    "CC BY 4.0",
                    "Eigener Name",
                    true,
                    false,
                    "adult"))).StatusCode;
            Assert.Equal(
                expectedResult35,
                actualResult36);

            // Existing publication candidates also need current individual text/image evidence.
            await using (var db = new LearnPipDbContext(options))
            {
                var original = preview.Version;
                var license = new { id = "CC BY 4.0", holder = "Eigener Name", attribution = "Eigener Text und eigenes Bild" };
                var evidence = new { license, provenance = new { kind = "original" } };
                db.QuestionRights.Add(new QuestionRights
                {
                    QuestionId = questionId,
                    ContentSha256 = await CatalogRightsEndpoints.Fingerprint(CatalogRightsEndpoints.VersionContent(original), db, CancellationToken.None),
                    PayloadJson = JsonSerializer.Serialize(new { license, evidence.provenance, media = new Dictionary<string, object> { [imageId.ToString()] = evidence } }),
                });
                await db.SaveChangesAsync();
            }

            var expectedResult37 = HttpStatusCode.Accepted;
            var actualResult38 = (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission",
                new PublicSubmissionInput(
                    preview.PreviewToken,
                    "CC BY 4.0",
                    "Eigener Name",
                    true,
                    true,
                    "adult"))).StatusCode;
            Assert.Equal(
                expectedResult37,
                actualResult38);
            var expectedResult39 = HttpStatusCode.NotFound;
            var actualResult40 = (await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult39,
                actualResult40);
            var expectedResult41 = HttpStatusCode.Forbidden;
            var actualResult42 = (await outsiderClient.GetAsync(
                "/api/v1/moderation/submissions/")).StatusCode;
            Assert.Equal(
                expectedResult41,
                actualResult42);
            var expectedResult43 = HttpStatusCode.OK;
            var actualResult44 = (await moderatorClient.GetAsync(
                $"/api/v1/moderation/submissions/{firstId}")).StatusCode;
            Assert.Equal(
                expectedResult43,
                actualResult44);
            var expectedResult45 = HttpStatusCode.OK;
            var actualResult46 = (await moderatorClient.GetAsync(
                $"/api/v1/moderation/submissions/{firstId}/media/{imageId}")).StatusCode;
            Assert.Equal(
                expectedResult45,
                actualResult46);
            var expectedResult47 = HttpStatusCode.NotFound;
            var actualResult48 = (await moderatorClient.GetAsync(
                $"/api/v1/moderation/submissions/{firstId}/media/{unrelatedImageId}")).StatusCode;
            Assert.Equal(
                expectedResult47,
                actualResult48);
            var expectedResult49 = HttpStatusCode.BadRequest;
            var actualResult50 = (await moderatorClient.PostAsJsonAsync(
                $"/api/v1/moderation/submissions/{firstId}/decision",
                new PublicReviewInput("approve", true, false, true, true, string.Empty))).StatusCode;
            Assert.Equal(
                expectedResult49,
                actualResult50);
            var expectedResult51 = HttpStatusCode.NoContent;
            var actualResult52 = (await moderatorClient.PostAsJsonAsync(
                $"/api/v1/moderation/submissions/{firstId}/decision",
                new PublicReviewInput("approve", true, true, true, true, "Geprüft"))).StatusCode;
            Assert.Equal(
                expectedResult51,
                actualResult52);
            var publicResponse = await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/1");
            Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
            var publicVersion = (await publicResponse.Content
                .ReadFromJsonAsync<ApiResponse<PublishedQuestionVersion>>())!.Data;
            Assert.Equal("CC BY 4.0", publicVersion.License);
            Assert.True(publicVersion.Rights.HasValue);
            Assert.Equal("Eigener Name", publicVersion.Rights.Value.GetProperty("license").GetProperty("holder").GetString());
            Assert.Equal("Eigener Name", publicVersion.AuthorAttribution);
            Assert.Equal("Eigener Text", publicVersion.Source);
            var expectedResult53 = HttpStatusCode.NotFound;
            var actualResult54 = (await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/2")).StatusCode;
            Assert.Equal(
                expectedResult53,
                actualResult54);
            var expectedResult55 = HttpStatusCode.OK;
            var actualResult56 = (await anonymous.GetAsync(
                $"/api/v1/public/media/{imageId}/content")).StatusCode;
            Assert.Equal(
                expectedResult55,
                actualResult56);
            var expectedResult57 = HttpStatusCode.NotFound;
            var actualResult58 = (await anonymous.GetAsync(
                $"/api/v1/public/media/{unrelatedImageId}/content")).StatusCode;
            Assert.Equal(
                expectedResult57,
                actualResult58);
            var expectedResult59 = HttpStatusCode.NoContent;
            var actualResult60 = (await ownerClient.PutAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/visibility",
                new VersionVisibilityInput("private"))).StatusCode;
            Assert.Equal(
                expectedResult59,
                actualResult60);
            var expectedResult61 = HttpStatusCode.NotFound;
            var actualResult62 = (await anonymous.GetAsync(
                $"/api/v1/public/media/{imageId}/content")).StatusCode;
            Assert.Equal(
                expectedResult61,
                actualResult62);
            var minorPreviewResponse = await ownerClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/2/submission-preview");
            var minorPreview = (await minorPreviewResponse.Content
                .ReadFromJsonAsync<ApiResponse<PublicPreview>>())!.Data;
            var expectedResult63 = HttpStatusCode.BadRequest;
            var actualResult64 = (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/2/submission",
                new PublicSubmissionInput(
                    minorPreview.PreviewToken,
                    "CC BY-SA 4.0",
                    "Eigener Name",
                    true,
                    false,
                    "minor"))).StatusCode;
            Assert.Equal(
                expectedResult63,
                actualResult64);
            var expectedResult65 = HttpStatusCode.OK;
            var actualResult66 = (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult65,
                actualResult66);
            var expectedResult67 = HttpStatusCode.NoContent;
            var actualResult68 = (await ownerClient.DeleteAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}")).StatusCode;
            Assert.Equal(
                expectedResult67,
                actualResult68);
            var expectedResult69 = HttpStatusCode.NotFound;
            var actualResult70 = (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode;
            Assert.Equal(
                expectedResult69,
                actualResult70);
        }
        finally
        {
            await using var admin = new NpgsqlConnection(maintenance.ConnectionString);
            await admin.OpenAsync();
            await using (var terminate = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @name AND pid <> pg_backend_pid()",
                admin))
            {
                terminate.Parameters.AddWithValue("name", name);
                await terminate.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<NewAccount> CreateAccount(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/pseudonymous", new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<NewAccount>>())!.Data;
    }

    private static HttpClient Client(
        WebApplicationFactory<Program> factory,
        string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed record IdView(Guid Id);
}
