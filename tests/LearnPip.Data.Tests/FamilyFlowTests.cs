// <copyright file="FamilyFlowTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LearnPip.Api;
using LearnPip.Api.Family;
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

/// <summary>
/// Enthält Regressionstests für die Familienverknüpfungen.
/// </summary>
public sealed class FamilyFlowTests
{
    /// <summary>
    /// Prüft bestätigte Familienverknüpfungen, begrenzte Aggregatdaten und den sofortigen Widerruf.
    /// </summary>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    [Fact]
    public async Task OnlyVerifiedChildConfirmedLinksExposeAggregatesAndRevocationIsImmediate()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var name = $"learnpip_family_test_{Guid.NewGuid():N}";
        var maintenance = new NpgsqlConnectionStringBuilder(source) { Database = "postgres" };
        var connection = new NpgsqlConnectionStringBuilder(source) { Database = name };
        await using (var admin = new NpgsqlConnection(maintenance.ConnectionString))
        {
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await command.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<LearnPipDbContext>()
                .UseNpgsql(connection.ConnectionString).Options;
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
            }

            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<LearnPipDbContext>>();
                    services.RemoveAll<IDbContextOptionsConfiguration<LearnPipDbContext>>();
                    services.AddDbContext<LearnPipDbContext>(dbOptions =>
                        dbOptions.UseNpgsql(connection.ConnectionString));
                }));
            using var anonymous = factory.CreateClient();
            var child = await Create(anonymous);
            var parent = await Create(anonymous);
            var stranger = await Create(anonymous);
            var administrator = await Create(anonymous);
            using var childClient = Client(factory, child.Session.Token);
            using var parentClient = Client(factory, parent.Session.Token);
            using var strangerClient = Client(factory, stranger.Session.Token);
            using var adminClient = Client(factory, administrator.Session.Token);
            const string root = "/api/v1/family";
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await childClient.PutAsJsonAsync($"{root}/age-band", new AgeBandInput("minor"))).StatusCode);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await parentClient.PutAsJsonAsync($"{root}/age-band", new AgeBandInput("adult"))).StatusCode);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await strangerClient.PutAsJsonAsync($"{root}/age-band", new AgeBandInput("adult"))).StatusCode);
            Assert.Equal(
                HttpStatusCode.Conflict,
                (await childClient.PutAsJsonAsync($"{root}/age-band", new AgeBandInput("adult"))).StatusCode);
            var invitation = await childClient.PostAsJsonAsync($"{root}/invites", new { });
            Assert.Equal(HttpStatusCode.Created, invitation.StatusCode);
            var created = (await invitation.Content.ReadFromJsonAsync<ApiResponse<InviteView>>())!.Data;
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"{root}/links/{created.Id}/overview")).StatusCode);
            Assert.Equal(
                HttpStatusCode.BadRequest,
                (await strangerClient.PostAsJsonAsync($"{root}/invites/redeem", new FamilyInviteInput("wrong"))).StatusCode);
            Assert.Equal(
                HttpStatusCode.Accepted,
                (await parentClient.PostAsJsonAsync($"{root}/invites/redeem", new FamilyInviteInput(created.Token))).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await strangerClient.PostAsJsonAsync($"{root}/invites/redeem", new FamilyInviteInput(created.Token))).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await parentClient.GetAsync($"{root}/links/{created.Id}/overview")).StatusCode);

            await using (var db = new LearnPipDbContext(options))
            {
                var role = new RoleDefinition
                {
                    Id = Guid.NewGuid(),
                    Scope = "system",
                    Code = "admin",
                    Name = "Administrator",
                };
                db.Roles.Add(role);
                db.AccountRoles.Add(new AccountRole
                {
                    AccountId = administrator.AccountId,
                    RoleDefinitionId = role.Id,
                });
                var question = new Question { OwnerAccountId = child.AccountId };
                db.Questions.Add(question);
                var version = new QuestionVersion
                {
                    QuestionId = question.Id,
                    CreatedByAccountId = child.AccountId,
                    VersionNumber = 1,
                    Prompt = "Private incorrect answer",
                };
                db.QuestionVersions.Add(version);
                db.PublicSubmissions.Add(new PublicSubmission
                {
                    QuestionVersionId = version.Id,
                    AccountId = child.AccountId,
                    Status = "minor_hold",
                    AgeDeclaration = "minor",
                });
                await db.SaveChangesAsync();
            }

            var expectedResult1 = HttpStatusCode.Forbidden;
            var actualResult2 = (await strangerClient.PostAsJsonAsync(
                $"/api/v1/admin/family/links/{created.Id}/verify",
                new FamilyVerificationInput("CASE-31"))).StatusCode;

            Assert.Equal(
                expectedResult1,
                actualResult2);
            var expectedResult3 = HttpStatusCode.NoContent;
            var actualResult4 = (await adminClient.PostAsJsonAsync(
                $"/api/v1/admin/family/links/{created.Id}/verify",
                new FamilyVerificationInput("CASE-31"))).StatusCode;
            Assert.Equal(
                expectedResult3,
                actualResult4);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await parentClient.GetAsync($"{root}/links/{created.Id}/overview")).StatusCode);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await childClient.PostAsJsonAsync($"{root}/links/{created.Id}/confirm", new { })).StatusCode);
            var overview = await parentClient.GetStringAsync($"{root}/links/{created.Id}/overview");
            Assert.Contains("completedSessionsLast28Days", overview);
            Assert.DoesNotContain("Private incorrect answer", overview);
            Assert.DoesNotContain("isCorrect", overview);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await strangerClient.GetAsync($"{root}/links/{created.Id}/overview")).StatusCode);
            Guid versionId;
            await using (var db = new LearnPipDbContext(options))
            {
                versionId = await db.PublicSubmissions.Select(item => item.QuestionVersionId).SingleAsync();
            }

            var expectedResult5 = HttpStatusCode.NotFound;
            var actualResult6 = (await strangerClient.PostAsJsonAsync(
                $"{root}/links/{created.Id}/submissions/{versionId}/approve",
                new { })).StatusCode;

            Assert.Equal(
                expectedResult5,
                actualResult6);
            var expectedResult7 = HttpStatusCode.NoContent;
            var actualResult8 = (await parentClient.PostAsJsonAsync(
                $"{root}/links/{created.Id}/submissions/{versionId}/approve",
                new { })).StatusCode;
            Assert.Equal(
                expectedResult7,
                actualResult8);
            Assert.Equal(
                HttpStatusCode.NoContent,
                (await childClient.PostAsJsonAsync($"{root}/links/{created.Id}/revoke", new { })).StatusCode);
            Assert.Equal(
                HttpStatusCode.NotFound,
                (await parentClient.GetAsync($"{root}/links/{created.Id}/overview")).StatusCode);
            await using (var db = new LearnPipDbContext(options))
            {
                Assert.Equal(6, await db.FamilyLinkEvents.CountAsync());
                Assert.Null((await db.PublicSubmissions.SingleAsync()).GuardianApprovedByAccountId);
                Assert.Equal("revoked", (await db.FamilyLinks.SingleAsync()).Status);
            }
        }
        finally
        {
            await using var admin = new NpgsqlConnection(maintenance.ConnectionString);
            await admin.OpenAsync();
            await using (var command = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @name AND pid <> pg_backend_pid()",
                admin))
            {
                command.Parameters.AddWithValue("name", name);
                await command.ExecuteNonQueryAsync();
            }

            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\"", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<NewAccount> Create(HttpClient client)
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

    private sealed record InviteView(Guid Id, string Token);
}
