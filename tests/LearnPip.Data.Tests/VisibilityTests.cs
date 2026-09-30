using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LearnPip.Api;
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

public sealed class VisibilityTests
{
    [Fact]
    public async Task Only_explicit_versions_and_their_referenced_media_are_visible()
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
            await using (var db = new LearnPipDbContext(options)) await db.Database.MigrateAsync();
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
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

            var catalogResponse = await ownerClient.PostAsJsonAsync("/api/v1/catalogs/",
                new { name = "Schule" });
            Assert.Equal(HttpStatusCode.Created, catalogResponse.StatusCode);
            var catalog = (await catalogResponse.Content.ReadFromJsonAsync<ApiResponse<IdView>>())!.Data;
            var groupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups/",
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
                        RoleDefinition = memberRole
                    },
                    new GroupMembership
                    {
                        StudyGroupId = group.Id,
                        AccountId = leader.AccountId,
                        RoleDefinition = leaderRole
                    });
                db.AccountRoles.AddRange(
                    new AccountRole { AccountId = moderator.AccountId, RoleDefinition = moderatorRole },
                    new AccountRole { AccountId = admin.AccountId, RoleDefinition = adminRole });
                db.Questions.Add(new Question
                {
                    Id = questionId,
                    OwnerAccountId = owner.AccountId,
                    PrivateCatalogId = catalog.Id
                });
                db.QuestionVersions.Add(new QuestionVersion
                {
                    Id = firstId,
                    QuestionId = questionId,
                    CreatedByAccountId = owner.AccountId,
                    VersionNumber = 1,
                    Prompt = "Erste Fassung",
                    Source = "Eigener Text"
                });
                foreach (var image in new[] { imageId, unrelatedImageId })
                {
                    db.MediaAssets.Add(new MediaAsset
                    {
                        Id = image,
                        OwnerAccountId = owner.AccountId,
                        StorageKey = $"private/{image:N}",
                        MediaType = "image/png",
                        ByteLength = 3
                    });
                    db.MediaBlobs.Add(new MediaBlob { MediaAssetId = image, Data = [1, 2, 3] });
                }
                db.QuestionContentBlocks.Add(new QuestionContentBlock
                {
                    QuestionVersionId = firstId,
                    Section = "prompt",
                    SortOrder = 0,
                    Kind = "image",
                    MediaAssetId = imageId
                });
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await moderatorClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await adminClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ownerClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.PutAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}", null)).StatusCode);

            await using (var db = new LearnPipDbContext(options))
            {
                db.QuestionVersions.Add(new QuestionVersion
                {
                    Id = secondId,
                    QuestionId = questionId,
                    CreatedByAccountId = owner.AccountId,
                    VersionNumber = 2,
                    Prompt = "Neue private Fassung",
                    Source = "Eigener Text"
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
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(
                    $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
                    $"/api/v1/questions/{questionId}/versions/2")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(
                    $"/api/v1/questions/{questionId}/attempts",
                    new GradeRequest(secondId, []))).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(
                    $"/api/v1/media/{imageId}/content")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
                    $"/api/v1/media/{unrelatedImageId}/content")).StatusCode);
            }
            foreach (var client in new[] { outsiderClient, moderatorClient, adminClient })
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(
                    $"/api/v1/media/{imageId}/content")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.PutAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/2")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await leaderClient.PutAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/visibility",
                new VersionVisibilityInput("public"))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await ownerClient.PutAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/visibility",
                new VersionVisibilityInput("public"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission",
                new PublicSubmissionInput("invalid", "CC BY 4.0", "Eigener Name",
                    true, true, "adult"))).StatusCode);
            var previewResponse = await ownerClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission-preview");
            Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
            var preview = (await previewResponse.Content.ReadFromJsonAsync<ApiResponse<PublicPreview>>())!.Data;
            Assert.True(preview.HasImages);
            Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission",
                new PublicSubmissionInput(preview.PreviewToken, "CC BY 4.0", "Eigener Name",
                    true, false, "adult"))).StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/submission",
                new PublicSubmissionInput(preview.PreviewToken, "CC BY 4.0", "Eigener Name",
                    true, true, "adult"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await outsiderClient.GetAsync(
                "/api/v1/moderation/submissions/")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await moderatorClient.GetAsync(
                $"/api/v1/moderation/submissions/{firstId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await moderatorClient.GetAsync(
                $"/api/v1/moderation/submissions/{firstId}/media/{imageId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await moderatorClient.GetAsync(
                $"/api/v1/moderation/submissions/{firstId}/media/{unrelatedImageId}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await moderatorClient.PostAsJsonAsync(
                $"/api/v1/moderation/submissions/{firstId}/decision",
                new PublicReviewInput("approve", true, false, true, true, ""))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await moderatorClient.PostAsJsonAsync(
                $"/api/v1/moderation/submissions/{firstId}/decision",
                new PublicReviewInput("approve", true, true, true, true, "Geprüft"))).StatusCode);
            var publicResponse = await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/1");
            Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
            var publicVersion = (await publicResponse.Content
                .ReadFromJsonAsync<ApiResponse<PublishedQuestionVersion>>())!.Data;
            Assert.Equal("CC BY 4.0", publicVersion.License);
            Assert.Equal("Eigener Name", publicVersion.AuthorAttribution);
            Assert.Equal("Eigener Text", publicVersion.Source);
            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(
                $"/api/v1/public/questions/{questionId}/versions/2")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anonymous.GetAsync(
                $"/api/v1/public/media/{imageId}/content")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(
                $"/api/v1/public/media/{unrelatedImageId}/content")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.PutAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/1/visibility",
                new VersionVisibilityInput("private"))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(
                $"/api/v1/public/media/{imageId}/content")).StatusCode);
            var minorPreviewResponse = await ownerClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/2/submission-preview");
            var minorPreview = (await minorPreviewResponse.Content
                .ReadFromJsonAsync<ApiResponse<PublicPreview>>())!.Data;
            Assert.Equal(HttpStatusCode.BadRequest, (await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{questionId}/versions/2/submission",
                new PublicSubmissionInput(minorPreview.PreviewToken, "CC BY-SA 4.0", "Eigener Name",
                    true, false, "minor"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
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

    private static async Task<NewAccount> CreateAccount(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/pseudonymous", new { });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<NewAccount>>())!.Data;
    }

    private static HttpClient Client(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private sealed record IdView(Guid Id);
}
