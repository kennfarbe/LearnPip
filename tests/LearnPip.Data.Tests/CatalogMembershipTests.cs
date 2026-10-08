// <copyright file="CatalogMembershipTests.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LearnPip.Api;
using LearnPip.Api.Questions;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace LearnPip.Data.Tests;

/// <summary>Prüft Mehrfachzuordnungen, Auswahl und Datenbestand gegen PostgreSQL.</summary>
public sealed partial class ApiV1Tests
{
    /// <summary>Mehrere Kataloge teilen dieselbe Frage und bewahren Lernstände und Eigentumsgrenzen.</summary>
    /// <returns>Die ausgeführte Integrationsprüfung.</returns>
    [Fact]
    public async Task CatalogMembershipsPreserveQuestionsAndFilterLearning()
    {
        var source = Environment.GetEnvironmentVariable("ConnectionStrings__LearnPip")
            ?? throw new InvalidOperationException("Set ConnectionStrings__LearnPip to a disposable PostgreSQL server.");
        var connection = new NpgsqlConnectionStringBuilder(source) { Database = "learnpip_memberships_" + Guid.NewGuid().ToString("N") };
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
            var stranger = Guid.NewGuid();
            var legacy = new PrivateCatalog { OwnerAccountId = owner, Name = "Alter Katalog" };
            var oldQuestion = new Question { OwnerAccountId = owner, PrivateCatalogId = legacy.Id };
            await using (var db = new LearnPipDbContext(options))
            {
                await db.Database.MigrateAsync();
                db.Accounts.AddRange(new Account { Id = owner }, new Account { Id = stranger });
                var adminRole = new RoleDefinition { Scope = "system", Code = "admin", Name = "Administrator" };
                db.Roles.Add(adminRole);
                db.AccountRoles.Add(new AccountRole { AccountId = stranger, RoleDefinitionId = adminRole.Id });
                db.PrivateCatalogs.Add(legacy);
                db.Questions.Add(oldQuestion);
                await db.SaveChangesAsync();
                await db.GetService<IMigrator>().MigrateAsync("20261008041231_IndividualRightsHistory");
                await db.Database.MigrateAsync();
                await db.Database.MigrateAsync();
                Assert.Equal(legacy.Id, (await db.QuestionCatalogMemberships.SingleAsync()).CatalogId);
            }

            using var factory = ExportFactory(connection.ConnectionString);
            using var client = ClientFor(factory, owner);
            using var other = ClientFor(factory, stranger);
            var first = await CreateMembershipCatalog(client, "Erster Katalog", " Beschreibung ");
            var second = await CreateMembershipCatalog(client, "Zweiter Katalog", null);
            var foreign = await CreateMembershipCatalog(other, "Fremder Katalog", null);
            var content = new QuestionPublishRequest(
                "single",
                "Mathematik",
                "Addition",
                "de",
                "Eigene Frage",
                "LicenseRef-Private",
                [new ContentBlockInput("text", "Was ergibt 2 + 2?", null)],
                [],
                [new AnswerInput(true, [new ContentBlockInput("text", "Vier", null)]),
                    new AnswerInput(false, [new ContentBlockInput("text", "Fünf", null)])]);
            using var created = await client.PostAsJsonAsync("/api/v1/questions/drafts", new DraftSaveRequest(content, null, [first, second]));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var question = (await created.Content.ReadFromJsonAsync<ApiResponse<DraftView>>())!.Data.QuestionId;
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync($"/api/v1/questions/{question}/publish", null)).StatusCode);
            using var outside = await client.PostAsJsonAsync("/api/v1/questions/drafts", new DraftSaveRequest(content, first));
            Assert.Equal(HttpStatusCode.Created, outside.StatusCode);
            var outsideId = (await outside.Content.ReadFromJsonAsync<ApiResponse<DraftView>>())!.Data.QuestionId;
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsync($"/api/v1/questions/{outsideId}/publish", null)).StatusCode);
            var draftPath = $"/api/v1/questions/{question}/draft";
            Assert.Equal(2, (await client.GetFromJsonAsync<ApiResponse<DraftView>>(draftPath))!.Data.CatalogIds!.Count);
            Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsync($"/api/v1/catalogs/{first}/questions/{question}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync($"/api/v1/catalogs/{foreign}/questions/{question}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/v1/catalogs/{first}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync(draftPath, new DraftSaveRequest(content, null, [first, foreign]))).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/v1/questions/drafts", new DraftSaveRequest(content, null, [first, first]))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/v1/catalogs/{first}", new CatalogInput("Erster Katalog", new string('x', 2049)))).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/v1/catalogs/{first}", new CatalogInput("Umbenannt", "Neue Beschreibung"))).StatusCode);
            var catalogs = (await client.GetFromJsonAsync<ApiResponse<CatalogView[]>>("/api/v1/catalogs/"))!.Data;
            Assert.Equal("Neue Beschreibung", catalogs.Single(item => item.Id == first).Description);
            Assert.Equal(1, catalogs.Single(item => item.Id == second).QuestionCount);
            Assert.Single((await client.GetFromJsonAsync<JsonElement>($"/api/v1/catalogs/{second}/questions")).GetProperty("data").EnumerateArray());
            var selection = (await client.GetFromJsonAsync<JsonElement>("/api/v1/catalogs/questions")).GetProperty("data");
            Assert.Equal(2, selection.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == question).GetProperty("catalogIds").GetArrayLength());
            using var started = await client.PostAsJsonAsync("/api/v1/learning/sessions/", new StartLearningRequest(second, 10));
            Assert.Equal(HttpStatusCode.Created, started.StatusCode);
            var session = (await started.Content.ReadFromJsonAsync<ApiResponse<LearningSessionView>>())!.Data;
            Assert.Equal(1, session.Total);
            Assert.Equal(question, session.Current!.QuestionId);
            using var dbCheck = new LearnPipDbContext(options);
            var version = await dbCheck.QuestionVersions.SingleAsync(item => item.QuestionId == question);
            var correct = await dbCheck.AnswerOptions.SingleAsync(item => item.QuestionVersionId == version.Id && item.IsCorrect);
            using var answered = await client.PostAsJsonAsync($"/api/v1/learning/sessions/{session.Id}/answer", new { selectedOptionIds = new[] { correct.Id }, wasGuessed = false });
            Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalogs/{first}/questions/{question}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalogs/{first}/questions/{question}")).StatusCode);
            Assert.Single((await client.GetFromJsonAsync<ApiResponse<DraftView>>(draftPath))!.Data.CatalogIds!);
            var additions = await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => client.PutAsync($"/api/v1/catalogs/{first}/questions/{question}", null)));
            Assert.All(additions, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalogs/{second}")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/catalogs/{first}")).StatusCode);
            await using var verify = new LearnPipDbContext(options);
            Assert.True(await verify.Questions.AnyAsync(item => item.Id == question && item.DeletedAtUtc == null));
            Assert.True(await verify.QuestionVersions.AnyAsync(item => item.Id == version.Id));
            Assert.True(await verify.StudyAttempts.AnyAsync(item => item.StudySessionId == session.Id));
            Assert.Empty(await verify.QuestionCatalogMemberships.Where(item => item.QuestionId == question).ToListAsync());
            Assert.Empty((await client.GetFromJsonAsync<ApiResponse<DraftView>>(draftPath))!.Data.CatalogIds!);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{connection.Database}\" WITH (FORCE)", server);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task<Guid> CreateMembershipCatalog(HttpClient client, string name, string? description)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/catalogs/", new CatalogInput(name, description));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var catalog = (await response.Content.ReadFromJsonAsync<ApiResponse<CatalogView>>())!.Data;
        Assert.Equal(description?.Trim(), catalog.Description);
        return catalog.Id;
    }
}
