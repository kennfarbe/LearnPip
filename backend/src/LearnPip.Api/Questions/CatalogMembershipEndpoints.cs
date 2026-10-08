// <copyright file="CatalogMembershipEndpoints.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Api.Questions;

/// <summary>Verwaltet Katalogmitgliedschaften ohne Kopien oder neue Lernstände.</summary>
public static class CatalogMembershipEndpoints
{
    /// <summary>Registriert Zuordnungen innerhalb eigener privater Kataloge.</summary>
    /// <param name="app">Die API-Routen.</param>
    /// <returns>Der Routen-Builder mit den registrierten Zuordnungen.</returns>
    public static IEndpointRouteBuilder MapCatalogMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        var routes = app.MapGroup("/api/v1/catalogs").RequireAuthorization(ApiPolicies.ActiveAccount)
            .RequireQuestionPermissions("editOwn");
        routes.MapPut("/{catalogId:guid}/questions/{questionId:guid}", Add);
        routes.MapDelete("/{catalogId:guid}/questions/{questionId:guid}", Remove);
        return app;
    }

    /// <summary>Prüft die komplette Auswahl vor jeglicher Änderung.</summary>
    /// <param name="db">Die Datenbank.</param>
    /// <param name="owner">Das Eigentümerkonto.</param>
    /// <param name="ids">Die ausdrücklich gewählten Kataloge.</param>
    /// <param name="ct">Das Abbruchtoken.</param>
    /// <returns>Ob alle ausgewählten Kataloge dem Konto gehören.</returns>
    internal static Task<bool> Validate(LearnPipDbContext db, Guid owner, Guid[] ids, CancellationToken ct) =>
        ids.Length > 100 || ids.Distinct().Count() != ids.Length ? Task.FromResult(false) :
        ValidateOwned(db, owner, ids, ct);

    /// <summary>Ersetzt die Zuordnungen; die Inhaltskennung und Lernstände bleiben erhalten.</summary>
    /// <param name="question">Die Frage samt geladenen Zuordnungen.</param>
    /// <param name="ids">Die vollständig geprüfte Auswahl.</param>
    internal static void Assign(Question question, Guid[] ids)
    {
        foreach (var membership in question.CatalogMemberships.Where(item => !ids.Contains(item.CatalogId)).ToArray())
        {
            question.CatalogMemberships.Remove(membership);
        }

        foreach (var id in ids.Where(id => question.CatalogMemberships.All(item => item.CatalogId != id)))
        {
            question.CatalogMemberships.Add(new QuestionCatalogMembership { CatalogId = id, Question = question });
        }

        question.PrivateCatalogId = ids.Length == 0 ? null : ids[0];
    }

    private static async Task<bool> ValidateOwned(LearnPipDbContext db, Guid owner, Guid[] ids, CancellationToken ct) =>
        await db.PrivateCatalogs.CountAsync(item => item.OwnerAccountId == owner && ids.Contains(item.Id), ct) == ids.Length;

    private static Task<IResult> Add(Guid catalogId, Guid questionId, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        Change(catalogId, questionId, true, db, user, ct);

    private static Task<IResult> Remove(Guid catalogId, Guid questionId, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        Change(catalogId, questionId, false, db, user, ct);

    private static async Task<IResult> Change(Guid catalogId, Guid questionId, bool add, LearnPipDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!AccountIdentity.TryGetAccountId(user, out var owner))
        {
            return Results.Unauthorized();
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM \"Questions\" WHERE \"Id\" = {questionId} AND \"OwnerAccountId\" = {owner} FOR UPDATE", ct);
        var question = await db.Questions.Include(item => item.CatalogMemberships)
            .SingleOrDefaultAsync(item => item.Id == questionId && item.OwnerAccountId == owner && item.DeletedAtUtc == null, ct);
        if (question == null || !await Validate(db, owner, [catalogId], ct))
        {
            return Results.NotFound();
        }

        var current = question.CatalogMemberships.Select(item => item.CatalogId)
            .Concat(question.PrivateCatalogId.HasValue ? [question.PrivateCatalogId.Value] : []).Distinct();
        var selected = add ? current.Append(catalogId).Distinct().ToArray() : current.Where(id => id != catalogId).ToArray();
        if (selected.Length > 100)
        {
            return Results.BadRequest();
        }

        Assign(question, selected);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Results.NoContent();
    }
}
