using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LearnPip.Api;
using LearnPip.Api.Groups;
using LearnPip.Api.Identity;
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

public sealed class GroupFlowTests
{
    [Fact]
    public async Task Invitations_are_limited_revocable_and_independent_of_membership()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var name = $"learnpip_group_test_{Guid.NewGuid():N}";
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        var database = new NpgsqlConnectionStringBuilder(source) { Database = name };
        await using (var admin = new NpgsqlConnection(maintenance.ConnectionString))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await command.ExecuteNonQueryAsync();
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
            var outsider = await CreateAccount(anonymous);
            using var ownerClient = Client(factory, owner.Session.Token);
            using var memberClient = Client(factory, member.Session.Token);
            using var outsiderClient = Client(factory, outsider.Session.Token);

            var catalogResponse = await ownerClient.PostAsJsonAsync("/api/v1/catalogs/",
                new { name = "Lernen" });
            Assert.Equal(HttpStatusCode.Created, catalogResponse.StatusCode);
            var catalog = (await catalogResponse.Content.ReadFromJsonAsync<ApiResponse<CatalogId>>())!.Data;
            var groupResponse = await ownerClient.PostAsJsonAsync("/api/v1/groups/",
                new GroupInput("Klasse A"));
            Assert.Equal(HttpStatusCode.Created, groupResponse.StatusCode);
            var group = (await groupResponse.Content.ReadFromJsonAsync<ApiResponse<GroupView>>())!.Data;

            var questionId = Guid.NewGuid();
            await using (var db = new LearnPipDbContext(options))
            {
                var question = new Question
                {
                    Id = questionId,
                    OwnerAccountId = owner.AccountId,
                    PrivateCatalogId = catalog.Id
                };
                db.Questions.Add(question);
                db.QuestionVersions.Add(new QuestionVersion
                {
                    QuestionId = question.Id,
                    CreatedByAccountId = owner.AccountId,
                    VersionNumber = 1,
                    Prompt = "Für die Gruppe"
                });
                await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.PutAsync(
                $"/api/v1/groups/{group.Id}/catalogs/{catalog.Id}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.GetAsync(
                $"/api/v1/groups/{group.Id}/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync(
                $"/api/v1/groups/{group.Id}/questions")).StatusCode);

            var inviteResponse = await ownerClient.PostAsJsonAsync(
                $"/api/v1/groups/{group.Id}/invitations",
                new InvitationInput(DateTimeOffset.UtcNow.AddDays(1), 1));
            Assert.Equal(HttpStatusCode.Created, inviteResponse.StatusCode);
            var invite = (await inviteResponse.Content
                .ReadFromJsonAsync<ApiResponse<InvitationView>>())!.Data;
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(
                "/api/v1/auth/recovery", new RecoveryRequest(invite.Code))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await memberClient.PostAsJsonAsync(
                "/api/v1/groups/join", new JoinInput(invite.Code))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.PostAsJsonAsync(
                "/api/v1/groups/join", new JoinInput(invite.Code))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync(
                $"/api/v1/groups/{group.Id}/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);

            var revokedResponse = await ownerClient.PostAsJsonAsync(
                $"/api/v1/groups/{group.Id}/invitations",
                new InvitationInput(DateTimeOffset.UtcNow.AddDays(1), 2));
            var revoked = (await revokedResponse.Content
                .ReadFromJsonAsync<ApiResponse<InvitationView>>())!.Data;
            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync(
                $"/api/v1/groups/{group.Id}/invitations/{revoked.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.PostAsJsonAsync(
                "/api/v1/groups/join", new JoinInput(revoked.Code))).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                await db.GroupInvitations.Where(item => item.Id == revoked.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc,
                        (DateTimeOffset?)null).SetProperty(item => item.ExpiresAtUtc,
                        DateTimeOffset.UtcNow.AddMinutes(-1)));
            }
            Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.PostAsJsonAsync(
                "/api/v1/groups/join", new JoinInput(revoked.Code))).StatusCode);

            Assert.Equal(HttpStatusCode.NoContent, (await ownerClient.DeleteAsync(
                $"/api/v1/groups/{group.Id}/members/{member.AccountId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync(
                $"/api/v1/groups/{group.Id}/questions")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await memberClient.GetAsync(
                $"/api/v1/questions/{questionId}/versions/1")).StatusCode);
        }
        finally
        {
            await using var admin = new NpgsqlConnection(maintenance.ConnectionString);
            await admin.OpenAsync();
            await using (var command = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @name AND pid <> pg_backend_pid()", admin))
            {
                command.Parameters.AddWithValue("name", name);
                await command.ExecuteNonQueryAsync();
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

    private sealed record CatalogId(Guid Id);
}
