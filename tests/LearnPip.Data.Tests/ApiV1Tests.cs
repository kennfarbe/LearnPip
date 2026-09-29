using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LearnPip.Api;
using LearnPip.Api.Questions;
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
            byte[] exif =
            [
                .. System.Text.Encoding.ASCII.GetBytes("Exif\0\0"),
                0x49, 0x49, 0x2a, 0x00, 0x08, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                .. System.Text.Encoding.ASCII.GetBytes("GPSDATA-private")
            ];
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
            Assert.True(contentResponse.Headers.CacheControl?.Private);
            Assert.True(contentResponse.Headers.CacheControl?.NoStore);
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

            using var questionImageUpload = new MultipartFormDataContent();
            var questionImage = new ByteArrayContent(jpeg);
            questionImage.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            questionImageUpload.Add(questionImage, "file", "answer.jpg");
            questionImageUpload.Add(new StringContent("Grünes Symbol"), "altText");
            var questionImageResponse = await ownerClient.PostAsync("/api/v1/media/", questionImageUpload);
            Assert.Equal(HttpStatusCode.Created, questionImageResponse.StatusCode);
            var questionMedia = (await questionImageResponse.Content
                .ReadFromJsonAsync<ApiResponse<MediaDetails>>())!.Data.Id;
            var draft = new QuestionPublishRequest("multiple", "Biologie", "Pflanzen", "de", "Eigene Frage",
                "CC-BY-4.0",
                [new ContentBlockInput("text", "Welche Aussagen treffen zu?", null),
                    new ContentBlockInput("image", null, questionMedia)],
                [new ContentBlockInput("text", "Grün ist richtig.", null)],
                [new AnswerInput(true, [new ContentBlockInput("image", null, questionMedia)]),
                    new AnswerInput(true, [new ContentBlockInput("text", "Chlorophyll", null)]),
                    new AnswerInput(false, [new ContentBlockInput("text", "Keine der beiden", null)])]);
            var created = await ownerClient.PostAsJsonAsync("/api/v1/questions/", draft);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var firstVersion = (await created.Content
                .ReadFromJsonAsync<ApiResponse<PublishedQuestionVersion>>())!.Data;
            var createdQuestionId = Guid.Parse(created.Headers.Location!.ToString().Split('/')[4]);
            Assert.Equal(1, firstVersion.Version);
            Assert.Contains(firstVersion.Prompt, block => block.MediaId == questionMedia);
            Assert.Contains(firstVersion.Answers[0].Blocks, block => block.MediaId == questionMedia);
            Assert.Equal(HttpStatusCode.Conflict,
                (await ownerClient.DeleteAsync($"/api/v1/media/{questionMedia}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/questions/{createdQuestionId}/versions/1")).StatusCode);
            var selected = firstVersion.Answers.Take(2).Select(answer => answer.Id).ToArray();
            var grade = await ownerClient.PostAsJsonAsync($"/api/v1/questions/{createdQuestionId}/attempts",
                new GradeRequest(firstVersion.Id, selected));
            Assert.Equal(HttpStatusCode.OK, grade.StatusCode);
            var firstGrade = (await grade.Content.ReadFromJsonAsync<ApiResponse<GradeResult>>())!.Data;
            Assert.True(firstGrade.IsCorrect);
            var partial = await ownerClient.PostAsJsonAsync($"/api/v1/questions/{createdQuestionId}/attempts",
                new GradeRequest(firstVersion.Id, [selected[0]]));
            Assert.False((await partial.Content.ReadFromJsonAsync<ApiResponse<GradeResult>>())!.Data.IsCorrect);
            var changedDraft = draft with
            {
                Answers = [new AnswerInput(false, draft.Answers[0].Blocks),
                    new AnswerInput(true, draft.Answers[1].Blocks),
                    new AnswerInput(true, draft.Answers[2].Blocks)]
            };
            var published = await ownerClient.PostAsJsonAsync(
                $"/api/v1/questions/{createdQuestionId}/versions", changedDraft);
            Assert.Equal(HttpStatusCode.Created, published.StatusCode);
            var secondVersion = (await published.Content
                .ReadFromJsonAsync<ApiResponse<PublishedQuestionVersion>>())!.Data;
            Assert.Equal(2, secondVersion.Version);
            Assert.False(secondVersion.Answers[0].IsCorrect);
            var oldVersion = await ownerClient.GetFromJsonAsync<ApiResponse<PublishedQuestionVersion>>(
                $"/api/v1/questions/{createdQuestionId}/versions/1");
            Assert.True(oldVersion!.Data.Answers[0].IsCorrect);
            await using (var checkAttempts = new LearnPipDbContext(options))
            {
                var saved = await checkAttempts.StudyAttempts.Include(item => item.Selections)
                    .SingleAsync(item => item.Id == firstGrade.AttemptId);
                Assert.Equal(firstVersion.Id, saved.QuestionVersionId);
                Assert.True(saved.IsCorrect);
                Assert.Equal(selected.Order(), saved.Selections.Select(item => item.AnswerOptionId).Order());
            }

            var catalogResponse = await ownerClient.PostAsJsonAsync("/api/v1/catalogs/",
                new CatalogInput("Prüfungsvorbereitung"));
            Assert.Equal(HttpStatusCode.Created, catalogResponse.StatusCode);
            var catalogId = (await catalogResponse.Content.ReadFromJsonAsync<ApiResponse<CatalogView>>())!.Data.Id;
            var emptyDraft = new QuestionPublishRequest("single", "", "", "de", "", "", [], [], []);
            var savedDraft = await ownerClient.PostAsJsonAsync("/api/v1/questions/drafts",
                new DraftSaveRequest(emptyDraft, catalogId));
            Assert.Equal(HttpStatusCode.Created, savedDraft.StatusCode);
            var privateDraftId = (await savedDraft.Content.ReadFromJsonAsync<ApiResponse<DraftView>>())!.Data.QuestionId;
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/questions/{privateDraftId}/draft")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/catalogs/{catalogId}/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await ownerClient.PostAsync($"/api/v1/questions/{privateDraftId}/publish", null)).StatusCode);
            await using (var checkPrivate = new LearnPipDbContext(options))
            {
                Assert.False(await checkPrivate.QuestionVersions.AnyAsync(item => item.QuestionId == privateDraftId));
            }
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.PutAsJsonAsync($"/api/v1/questions/{privateDraftId}/draft",
                    new DraftSaveRequest(draft, catalogId))).StatusCode);
            Assert.Equal(HttpStatusCode.Created,
                (await ownerClient.PostAsync($"/api/v1/questions/{privateDraftId}/publish", null)).StatusCode);
            var learningStart = await ownerClient.PostAsJsonAsync("/api/v1/learning/sessions/",
                new StartLearningRequest(catalogId, 5));
            Assert.Equal(HttpStatusCode.Created, learningStart.StatusCode);
            var learning = (await learningStart.Content
                .ReadFromJsonAsync<ApiResponse<LearningSessionView>>())!.Data;
            Assert.Equal(1, learning.Total);
            Assert.NotNull(learning.Current);
            Assert.Equal(privateDraftId, learning.Current.QuestionId);
            Assert.Equal(3, learning.Current.Answers.Select(option => option.Id).Distinct().Count());
            Assert.DoesNotContain("isCorrect", await learningStart.Content.ReadAsStringAsync(),
                StringComparison.OrdinalIgnoreCase);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"/api/v1/learning/sessions/{learning.Id}")).StatusCode);
            var partialLearning = await ownerClient.PostAsJsonAsync(
                $"/api/v1/learning/sessions/{learning.Id}/answer",
                new LearningAnswerRequest([learning.Current.Answers[0].Id]));
            Assert.Equal(HttpStatusCode.OK, partialLearning.StatusCode);
            Assert.False((await partialLearning.Content
                .ReadFromJsonAsync<ApiResponse<LearningFeedback>>())!.Data.IsCorrect);
            Assert.Equal(HttpStatusCode.Conflict,
                (await ownerClient.PostAsJsonAsync($"/api/v1/learning/sessions/{learning.Id}/answer",
                    new LearningAnswerRequest([learning.Current.Answers[0].Id]))).StatusCode);
            var finishedLearning = (await ownerClient.GetFromJsonAsync<ApiResponse<LearningSessionView>>(
                $"/api/v1/learning/sessions/{learning.Id}"))!.Data;
            Assert.True(finishedLearning.Completed);
            Assert.Equal(1, finishedLearning.Answered);
            Assert.Equal(0, finishedLearning.Skipped);
            Assert.Null(finishedLearning.Current);

            var correctStart = await ownerClient.PostAsJsonAsync("/api/v1/learning/sessions/",
                new StartLearningRequest(catalogId, 1));
            var correctSession = (await correctStart.Content
                .ReadFromJsonAsync<ApiResponse<LearningSessionView>>())!.Data;
            var correctVersion = (await ownerClient.GetFromJsonAsync<ApiResponse<PublishedQuestionVersion>>(
                $"/api/v1/questions/{privateDraftId}/versions/1"))!.Data;
            var correctLearning = await ownerClient.PostAsJsonAsync(
                $"/api/v1/learning/sessions/{correctSession.Id}/answer",
                new LearningAnswerRequest(correctVersion.Answers.Where(option => option.IsCorrect)
                    .Select(option => option.Id).ToArray()));
            Assert.True((await correctLearning.Content
                .ReadFromJsonAsync<ApiResponse<LearningFeedback>>())!.Data.IsCorrect);
            var correctFeedback = (await correctLearning.Content
                .ReadFromJsonAsync<ApiResponse<LearningFeedback>>())!.Data;
            var beforeMark = (await ownerClient.GetFromJsonAsync<ApiResponse<ReviewOverview>>(
                "/api/v1/learning/review"))!.Data;
            Assert.Contains(beforeMark.Contents, item => item.Id == correctFeedback.ContentId &&
                item.Answers == 2);
            Assert.Equal(HttpStatusCode.NotFound,
                (await strangerClient.PutAsync($"/api/v1/learning/contents/{correctFeedback.ContentId}/often-for-me",
                    null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.PutAsync($"/api/v1/learning/contents/{correctFeedback.ContentId}/often-for-me",
                    null)).StatusCode);
            var afterMark = (await ownerClient.GetFromJsonAsync<ApiResponse<ReviewOverview>>(
                "/api/v1/learning/review"))!.Data;
            Assert.Equal(beforeMark.MasteredContents, afterMark.MasteredContents);
            Assert.Equal(beforeMark.TotalContents, afterMark.TotalContents);
            Assert.Equal(1, afterMark.OftenForMeCount);
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.PostAsJsonAsync(
                    $"/api/v1/learning/sessions/{correctSession.Id}/explanation",
                    new ExplanationViewRequest(correctFeedback.AttemptId))).StatusCode);
            var explained = (await ownerClient.GetFromJsonAsync<ApiResponse<ReviewOverview>>(
                "/api/v1/learning/review"))!.Data;
            Assert.Equal(1, explained.Contents.Single(item => item.Id == correctFeedback.ContentId)
                .ExplanationsViewed);

            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.PutAsJsonAsync(
                    $"/api/v1/learning/questions/{createdQuestionId}/content",
                    new ContentAssignment(correctFeedback.ContentId))).StatusCode);
            var grouped = (await ownerClient.GetFromJsonAsync<ApiResponse<ReviewOverview>>(
                "/api/v1/learning/review"))!.Data;
            Assert.Equal(afterMark.TotalContents - 1, grouped.TotalContents);
            Assert.Equal(2, grouped.Contents.Single(item => item.Id == correctFeedback.ContentId)
                .QuestionIds.Count);

            var mixedStart = await ownerClient.PostAsJsonAsync("/api/v1/learning/sessions/",
                new StartLearningRequest(null, 10));
            var mixed = (await mixedStart.Content
                .ReadFromJsonAsync<ApiResponse<LearningSessionView>>())!.Data;
            Assert.True(mixed.Total >= 2);
            var seen = new HashSet<Guid>();
            while (mixed.Current != null)
            {
                Assert.True(seen.Add(mixed.Current.QuestionId));
                Assert.Equal(HttpStatusCode.NoContent,
                    (await ownerClient.PostAsync($"/api/v1/learning/sessions/{mixed.Id}/skip", null)).StatusCode);
                mixed = (await ownerClient.GetFromJsonAsync<ApiResponse<LearningSessionView>>(
                    $"/api/v1/learning/sessions/{mixed.Id}"))!.Data;
            }
            Assert.True(mixed.Completed);
            Assert.Equal(0, mixed.Answered);
            Assert.Equal(mixed.Total, mixed.Skipped);
            await using (var checkSkipped = new LearnPipDbContext(options))
                Assert.False(await checkSkipped.StudyAttempts.AnyAsync(item => item.StudySessionId == mixed.Id));

            var catalogItems = await ownerClient.GetStringAsync($"/api/v1/catalogs/{catalogId}/questions");
            Assert.Contains(privateDraftId.ToString(), catalogItems);
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.PutAsJsonAsync($"/api/v1/questions/{privateDraftId}/catalog",
                    new CatalogMoveRequest(null))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent,
                (await ownerClient.DeleteAsync($"/api/v1/catalogs/{catalogId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound,
                (await ownerClient.GetAsync($"/api/v1/catalogs/{catalogId}/questions")).StatusCode);

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
                var media = await db.MediaAssets.SingleAsync(item => item.Id == mediaId);
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
