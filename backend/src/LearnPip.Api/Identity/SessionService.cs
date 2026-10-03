// <copyright file="SessionService.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Api.Security;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnPip.Api.Identity;

/// <summary>
/// Erstellt und widerruft Kontositzungen.
/// </summary>
/// <param name="dbContext">Der Datenbankkontext.</param>
public sealed class SessionService(LearnPipDbContext dbContext)
{
    /// <summary>
    /// Erstellt eine neue Sitzung oder Lerngruppe mit den angegebenen Kontodaten.
    /// </summary>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public async Task<SessionGrant> CreateAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var token = SessionAuthentication.NewSecret();
        var expires = DateTimeOffset.UtcNow.AddDays(30);
        dbContext.AccountSessions.Add(new AccountSession
        {
            AccountId = accountId,
            TokenHash = SessionAuthentication.Hash(token),
            ExpiresAtUtc = expires,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SessionGrant(token, expires);
    }

    /// <summary>
    /// Widerruft die angegebene Sitzung eines Kontos.
    /// </summary>
    /// <param name="sessionId">Die Kennung der Sitzung.</param>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<int> RevokeAsync(Guid sessionId, Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.AccountSessions
            .Where(session => session.Id == sessionId && session.AccountId == accountId &&
                              session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
        setters => setters.SetProperty(
            session => session.RevokedAtUtc,
            DateTimeOffset.UtcNow),
        cancellationToken);

    /// <summary>
    /// Widerruft alle aktiven Sitzungen eines Kontos.
    /// </summary>
    /// <param name="accountId">Die Kennung des betroffenen Kontos.</param>
    /// <param name="cancellationToken">Das Token zum Abbrechen der Operation.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public Task<int> RevokeAllAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.AccountSessions
            .Where(session => session.AccountId == accountId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
        setters => setters.SetProperty(
            session => session.RevokedAtUtc,
            DateTimeOffset.UtcNow),
        cancellationToken);
}
