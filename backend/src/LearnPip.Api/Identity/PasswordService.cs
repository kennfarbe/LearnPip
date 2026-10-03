// <copyright file="PasswordService.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LearnPip.Api.Identity;

/// <summary>Verwaltet lokale Passwortzugänge mit transaktionalem Bootstrap und Sitzungswiderruf.</summary>
/// <param name="db">Der Datenbankkontext.</param>
/// <param name="sessions">Die Sitzungsverwaltung.</param>
public sealed class PasswordService(LearnPipDbContext db, SessionService sessions)
{
    private static readonly PasswordHasher<PasswordCredential> Hasher = new(
        Options.Create(new PasswordHasherOptions { IterationCount = 600000 }));

    private static readonly string DummyHash = Hasher.HashPassword(new PasswordCredential(), SessionAuthentication.NewSecret());

    /// <summary>Prüft die verbindlichen Passwortanforderungen ohne stille Kürzung.</summary>
    /// <param name="password">Das zu prüfende Passwort.</param>
    /// <returns>Ob das Passwort zulässig ist.</returns>
    public static bool IsValidPassword(string? password) => password is { Length: >= 12 and <= 128 } &&
        !string.IsNullOrWhiteSpace(password) && !password.Contains('\0');

    /// <summary>Normalisiert einen ASCII-Benutzernamen.</summary>
    /// <param name="username">Der eingegebene Benutzername.</param>
    /// <returns>Der normalisierte Name oder null.</returns>
    public static string? NormalizeUsername(string? username)
    {
        var value = username?.Trim().ToLowerInvariant();
        return value is { Length: >= 1 and <= 64 } &&
            value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.')
            ? value : null;
    }

    /// <summary>Richtet den ersten Administrator einmalig unter derselben Sperre wie der bisherige Bootstrap ein.</summary>
    /// <param name="username">Der gewählte Benutzername.</param>
    /// <param name="password">Das Passwort aus der geschützten Eingabe.</param>
    /// <param name="cancellationToken">Das Abbruchtoken.</param>
    /// <returns>Ob ein Konto neu eingerichtet wurde.</returns>
    public async Task<bool> BootstrapAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUsername(username) ?? throw new ArgumentException("Ungültiger Benutzername.", nameof(username));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await this.LockAsync(cancellationToken);
        if (await db.SystemSettings.AnyAsync(item => item.Key == "admin_bootstrapped", cancellationToken) ||
            await db.AccountRoles.AnyAsync(item => item.RoleDefinition.Scope == "system" && item.RoleDefinition.Code == "admin", cancellationToken))
        {
            return false;
        }

        ArgumentOutOfRangeException.ThrowIfEqual(IsValidPassword(password), false);
        var account = new Account { DisplayName = normalized };
        var credential = new PasswordCredential { Account = account, Username = normalized };
        credential.PasswordHash = Hasher.HashPassword(credential, password);
        var role = await db.Roles.SingleOrDefaultAsync(item => item.Scope == "system" && item.Code == "admin", cancellationToken);
        if (role == null)
        {
            role = new RoleDefinition { Scope = "system", Code = "admin", Name = "Administrator" };
            db.Roles.Add(role);
        }

        db.PasswordCredentials.Add(credential);
        db.AccountRoles.Add(new AccountRole { Account = account, RoleDefinition = role });
        db.SystemSettings.Add(new SystemSetting { Key = "admin_bootstrapped", Value = "true" });
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent { Action = "admin.bootstrap", Target = $"account:{account.Id}", NewValue = "admin" });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Prüft den Zugang und erstellt eine frische Sitzung ohne Benutzerermittlung über Fehlermeldungen.</summary>
    /// <param name="username">Der Benutzername.</param>
    /// <param name="password">Das Passwort.</param>
    /// <param name="cancellationToken">Das Abbruchtoken.</param>
    /// <returns>Die neue Sitzung oder null.</returns>
    public async Task<SessionGrant?> SignInAsync(string? username, string? password, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await this.LockAsync(cancellationToken);
        var normalized = NormalizeUsername(username);
        var credential = await db.PasswordCredentials.Include(item => item.Account)
            .SingleOrDefaultAsync(item => item.Username == normalized, cancellationToken);
        var valid = Verify(credential, password);
        var now = DateTimeOffset.UtcNow;
        if (credential == null || credential.Account.DeletedAtUtc != null)
        {
            return null;
        }

        if (credential.LockedUntilUtc > now)
        {
            return null;
        }

        if (!valid)
        {
            credential.FailedAttempts = credential.LockedUntilUtc.HasValue ? 1 : credential.FailedAttempts + 1;
            credential.LockedUntilUtc = credential.FailedAttempts >= 5 ? now.AddMinutes(15) : null;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        credential.FailedAttempts = 0;
        credential.LockedUntilUtc = null;
        credential.Account.DisabledAtUtc = null;
        credential.Account.LastActivityAtUtc = now;
        credential.Account.UpdatedAtUtc = now;
        credential.PasswordHash = Hasher.HashPassword(credential, password!);
        var grant = await sessions.CreateAsync(credential.AccountId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return grant;
    }

    /// <summary>Ändert das Passwort nach erneuter Prüfung und widerruft sämtliche Sitzungen.</summary>
    /// <param name="accountId">Das angemeldete Konto.</param>
    /// <param name="sessionId">Die erneut geprüfte aktive Sitzung.</param>
    /// <param name="currentPassword">Das bisherige Passwort.</param>
    /// <param name="newPassword">Das neue Passwort.</param>
    /// <param name="cancellationToken">Das Abbruchtoken.</param>
    /// <returns>Ob das Passwort geändert wurde.</returns>
    public async Task<bool> ChangeAsync(Guid accountId, Guid sessionId, string? currentPassword, string? newPassword, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await this.LockAsync(cancellationToken);
        var credential = await db.PasswordCredentials.Include(item => item.Account)
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        var valid = Verify(credential, currentPassword);
        var now = DateTimeOffset.UtcNow;
        if (credential == null || credential.Account.DeletedAtUtc != null || credential.LockedUntilUtc > now ||
            !await db.AccountSessions.AnyAsync(
                item => item.Id == sessionId && item.AccountId == accountId &&
                    item.RevokedAtUtc == null && item.ExpiresAtUtc > now,
                cancellationToken))
        {
            return false;
        }

        if (!valid)
        {
            credential.FailedAttempts = credential.LockedUntilUtc.HasValue ? 1 : credential.FailedAttempts + 1;
            credential.LockedUntilUtc = credential.FailedAttempts >= 5 ? now.AddMinutes(15) : null;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return false;
        }

        if (!IsValidPassword(newPassword))
        {
            return false;
        }

        await this.ReplaceAsync(credential, newPassword!, "password.change", accountId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Lokaler Wiederherstellungsweg mit Datenbankzugang; verändert keine Rollen.</summary>
    /// <param name="username">Der bestehende Benutzername.</param>
    /// <param name="newPassword">Das neue Passwort.</param>
    /// <param name="cancellationToken">Das Abbruchtoken.</param>
    /// <returns>Die asynchrone Operation.</returns>
    public async Task ResetAsync(string username, string newPassword, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUsername(username) ?? throw new ArgumentException("Ungültiger Benutzername.", nameof(username));
        ArgumentOutOfRangeException.ThrowIfEqual(IsValidPassword(newPassword), false);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await this.LockAsync(cancellationToken);
        var credential = await db.PasswordCredentials.Include(item => item.Account).SingleAsync(
            item => item.Username == normalized && item.Account.DeletedAtUtc == null, cancellationToken);
        await this.ReplaceAsync(credential, newPassword, "password.local_reset", null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static bool Verify(PasswordCredential? credential, string? password)
    {
        var input = password is { Length: <= 128 } ? password : string.Empty;
        return Hasher.VerifyHashedPassword(credential ?? new PasswordCredential(), credential?.PasswordHash ?? DummyHash, input) !=
            PasswordVerificationResult.Failed && password is { Length: > 0 and <= 128 };
    }

    private async Task ReplaceAsync(PasswordCredential credential, string password, string action, Guid? actorId, CancellationToken cancellationToken)
    {
        credential.PasswordHash = Hasher.HashPassword(credential, password);
        credential.FailedAttempts = 0;
        credential.LockedUntilUtc = null;
        await sessions.RevokeAllAsync(credential.AccountId, cancellationToken);
        db.AdministrationAuditEvents.Add(new AdministrationAuditEvent { ActorAccountId = actorId, Action = action, Target = $"account:{credential.AccountId}" });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task LockAsync(CancellationToken cancellationToken) =>
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1049071810)", cancellationToken);
}
