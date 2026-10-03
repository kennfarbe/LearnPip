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

public sealed class SessionService(LearnPipDbContext dbContext)
{
    public async Task<SessionGrant> CreateAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var token = SessionAuthentication.NewSecret();
        var expires = DateTimeOffset.UtcNow.AddDays(30);
        dbContext.AccountSessions.Add(new AccountSession
        {
            AccountId = accountId,
            TokenHash = SessionAuthentication.Hash(token),
            ExpiresAtUtc = expires
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return new SessionGrant(token, expires);
    }

    public Task<int> RevokeAsync(Guid sessionId, Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.AccountSessions
            .Where(session => session.Id == sessionId && session.AccountId == accountId &&
                              session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                session => session.RevokedAtUtc, DateTimeOffset.UtcNow), cancellationToken);

    public Task<int> RevokeAllAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.AccountSessions
            .Where(session => session.AccountId == accountId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                session => session.RevokedAtUtc, DateTimeOffset.UtcNow), cancellationToken);
}
