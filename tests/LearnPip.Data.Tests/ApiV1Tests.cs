using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LearnPip.Api;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using SkiaSharp;

namespace LearnPip.Data.Tests;

public sealed class ApiV1Tests
{
    [Fact]
    public async Task Private_resources_require_the_owner_or_explicit_group_share()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var databaseName = $"learnpip_api_test_{Guid.NewGuid():N}";
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
            var owner = Guid.NewGuid();
            var member = Guid.NewGuid();
            var stranger = Guid.NewGuid();
            var mediaId = Guid.NewGuid();
            var questionId = Guid.NewGuid();
            var groupId = Guid.NewGuid();
            var options = new DbContextOptionsBuilder<LearnPipDbContext>()
                .UseNpgsql(connection.ConnectionString).Options;
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Accounts.AddRange(
                    new Account { Id = owner },
                    new Account { Id = member },
                    new Account { Id = stranger });
                db.Questions.Add(new Question { Id = questionId, OwnerAccountId = owner });
                db.QuestionVersions.Add(new QuestionVersion
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    CreatedByAccountId = owner,
                    VersionNumber = 1,
                    Prompt = "Private prompt"
                });
                db.MediaAssets.Add(new MediaAsset
                {
                    Id = mediaId,
                    OwnerAccountId = owner,
                    StorageKey = "private/secret-object-key",
                    MediaType = "image/jpeg",
                    ByteLength = 123
                });
                var groupRole = new RoleDefinition
                {
                    Id = Guid.NewGuid(),
                    Scope = "group",
                    Code = "member",
                    Name = "Member"
                };
                db.Roles.Add(groupRole);
                db.StudyGroups.Add(new StudyGroup { Id = groupId, OwnerAccountId = owner, Name = "Study" });
                db.GroupMemberships.Add(new GroupMembership
                {
                    StudyGroupId = groupId,
                    AccountId = member,
                    RoleDefinitionId = groupRole.Id
                });
                db.GroupQuestionShares.Add(new GroupQuestionShare
                {
                    StudyGroupId = groupId,
                    QuestionId = questionId,
                    SharedByAccountId = owner
                });
                var moderatorRole = new RoleDefinition
                {
                    Id = Guid.NewGuid(),
                    Scope = "system",
                    Code = "moderator",
                    Name = "Moderator"
                };
                db.Roles.Add(moderatorRole);
                db.AccountRoles.Add(new AccountRole
                {
                    AccountId = member,
                    RoleDefinitionId = moderatorRole.Id
                });
                await db.SaveChangesAsync();
            }

            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(webBuilder =>
                {
                    webBuilder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<DbContextOptions<LearnPipDbContext>>();
                        services.RemoveAll<IDbContextOptionsConfiguration<LearnPipDbContext>>();
                        services.AddDbContext<LearnPipDbContext>(options =>
                            options.UseNpgsql(connection.ConnectionString));
                        services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
                            options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
                            options.DefaultForbidScheme = TestAuthenticationHandler.Scheme;
                        }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            TestAuthenticationHandler.Scheme, _ => { });
                    });
                });

            using (var dbScope = factory.Services.CreateScope())
            {
                var apiDb = dbScope.ServiceProvider.GetRequiredService<LearnPipDbContext>();
                Assert.Equal(databaseName, apiDb.Database.GetDbConnection().Database);
            }

            using var anonymous = factory.CreateClient();
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonymous.GetAsync($"/api/v1/questions/{questionId}")).StatusCode);

            using var ownerClient = ClientFor(factory, owner);
            using var memberClient = ClientFor(factory, member);
            using var strangerClient = ClientFor(factory, stranger);
            Assert.Equal(HttpStatusCode.OK,
                (await ownerClient.GetAsync($"/api/v1/questions/{questionId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/questions/{questionId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK,
                (await memberClient.GetAsync($"/api/v1/questions/{questionId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/media/{mediaId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await memberClient.GetAsync($"/api/v1/media/{mediaId}")).StatusCode);
            var mediaResponse = await ownerClient.GetAsync($"/api/v1/media/{mediaId}");
            Assert.Equal(HttpStatusCode.OK, mediaResponse.StatusCode);
            Assert.DoesNotContain("secret-object-key", await mediaResponse.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK,
                (await memberClient.GetAsync($"/api/v1/groups/{groupId}/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/groups/{groupId}/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await ownerClient.GetAsync("/api/v1/questions?pageSize=101")).StatusCode);

            var ownPage = await ownerClient.GetFromJsonAsync<ApiResponse<PageResponse<QuestionSummary>>>(
                "/api/v1/questions?page=1&pageSize=10");
            Assert.Equal(questionId, Assert.Single(ownPage!.Data.Items).Id);
            var otherPage = await strangerClient.GetFromJsonAsync<ApiResponse<PageResponse<QuestionSummary>>>(
                "/api/v1/questions");
            Assert.Empty(otherPage!.Data.Items);

            var contract = await anonymous.GetStringAsync("/openapi/v1.json");
            Assert.Contains("/api/v1/questions", contract);
            Assert.Contains("/api/v1/media/{id}", contract);

            using var sourceBitmap = new SKBitmap(2, 2);
            sourceBitmap.Erase(SKColors.Green);
            using var sourceJpeg = sourceBitmap.Encode(SKEncodedImageFormat.Jpeg, 85);
            var jpeg = sourceJpeg.ToArray();
            var exif = System.Text.Encoding.ASCII.GetBytes("Exif\0\0GPSDATA-private");
            using var withExif = new MemoryStream();
            withExif.Write(jpeg, 0, 2);
            withExif.Write([0xff, 0xe1, (byte)((exif.Length + 2) >> 8), (byte)(exif.Length + 2)]);
            withExif.Write(exif);
            withExif.Write(jpeg, 2, jpeg.Length - 2);
            using var upload = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(withExif.ToArray());
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            upload.Add(imageContent, "file", "photo.jpg");
            upload.Add(new StringContent("Grünes Quadrat"), "altText");
            var uploadResponse = await ownerClient.PostAsync("/api/v1/media/", upload);
            Assert.True(uploadResponse.StatusCode == HttpStatusCode.Created,
                await uploadResponse.Content.ReadAsStringAsync());
            var uploaded = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<MediaDetails>>();
            var uploadedId = uploaded!.Data.Id;
            Assert.Equal("Grünes Quadrat", uploaded.Data.AltText);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/media/{uploadedId}/content")).StatusCode);
            var contentResponse = await ownerClient.GetAsync($"/api/v1/media/{uploadedId}/content");
            Assert.Equal(HttpStatusCode.OK, contentResponse.StatusCode);
            Assert.Equal("private, no-store", contentResponse.Headers.CacheControl?.ToString());
            Assert.DoesNotContain("GPSDATA-private", System.Text.Encoding.Latin1.GetString(
                await contentResponse.Content.ReadAsByteArrayAsync()));
            using var invalidUpload = new MultipartFormDataContent();
            var invalidImage = new ByteArrayContent([1, 2, 3, 4]);
            invalidImage.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            invalidUpload.Add(invalidImage, "file", "bad.jpg");
            invalidUpload.Add(new StringContent("Ungültig"), "altText");
            Assert.Equal(HttpStatusCode.BadRequest,
                (await ownerClient.PostAsync("/api/v1/media/", invalidUpload)).StatusCode);
            using var tooLargeUpload = new MultipartFormDataContent();
            var tooLargeImage = new ByteArrayContent(new byte[5 * 1024 * 1024 + 1]);
            tooLargeImage.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            tooLargeUpload.Add(tooLargeImage, "file", "large.jpg");
            tooLargeUpload.Add(new StringContent("Zu groß"), "altText");
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge,
                (await ownerClient.PostAsync("/api/v1/media/", tooLargeUpload)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.DeleteAsync($"/api/v1/media/{uploadedId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await ownerClient.GetAsync($"/api/v1/media/{uploadedId}/content")).StatusCode);
            await using (var checkDb = new LearnPipDbContext(options))
            {
                Assert.False(await checkDb.MediaAssets.AnyAsync(item => item.Id == uploadedId));
                Assert.False(await checkDb.MediaBlobs.AnyAsync(item => item.MediaAssetId == uploadedId));
            }

            using var scope = factory.Services.CreateScope();
            var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
            var memberPrincipal = PrincipalFor(member);
            Assert.True((await authorization.AuthorizeAsync(memberPrincipal, null, ApiPolicies.Moderation)).Succeeded);
            Assert.False((await authorization.AuthorizeAsync(memberPrincipal, null, ApiPolicies.Admin)).Succeeded);
            Assert.False((await authorization.AuthorizeAsync(PrincipalFor(stranger), null, ApiPolicies.Moderation)).Succeeded);

            await using (var db = new LearnPipDbContext(options))
            {
                var share = await db.GroupQuestionShares.SingleAsync();
                share.RevokedAtUtc = DateTimeOffset.UtcNow;
                var media = await db.MediaAssets.SingleAsync();
                media.DeletedAtUtc = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }

            Assert.Equal(HttpStatusCode.NotFound,
                (await memberClient.GetAsync($"/api/v1/questions/{questionId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await ownerClient.GetAsync($"/api/v1/media/{mediaId}")).StatusCode);
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

    private static HttpClient ClientFor(WebApplicationFactory<Program> factory, Guid accountId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Account", accountId.ToString());
        return client;
    }

    private static ClaimsPrincipal PrincipalFor(Guid accountId) =>
        new(new ClaimsIdentity(
            [new Claim(AccountIdentity.AccountIdClaim, accountId.ToString())],
            TestAuthenticationHandler.Scheme));

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Scheme = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Account", out var accountId) ||
                !Guid.TryParse(accountId, out var parsed))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var ticket = new AuthenticationTicket(PrincipalFor(parsed), Scheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
