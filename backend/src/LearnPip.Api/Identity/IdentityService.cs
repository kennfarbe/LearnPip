using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using LearnPip.Data;
using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearnPip.Api.Identity;

public sealed class IdentityConflictException : Exception;

public interface IEmailCodeSender
{
    bool IsAvailable { get; }
    Task SendAsync(string email, string code, CancellationToken cancellationToken);
}

public sealed class SmtpEmailCodeSender(IConfiguration configuration) : IEmailCodeSender
{
    public bool IsAvailable =>
        !string.IsNullOrWhiteSpace(configuration["Mail:Host"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:From"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:Username"]) &&
        !string.IsNullOrWhiteSpace(configuration["Mail:Password"]);

    public async Task SendAsync(string email, string code, CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Mail transport is not configured.");
        }

        var host = configuration["Mail:Host"]!;
        var port = int.TryParse(configuration["Mail:Port"], out var configuredPort)
            ? configuredPort : 587;
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(
                configuration["Mail:Username"], configuration["Mail:Password"])
        };
        using var message = new MailMessage(configuration["Mail:From"]!, email)
        {
            Subject = "LearnPip Anmeldecode",
            Body = $"Dein einmaliger LearnPip-Code lautet: {code}\nEr ist 10 Minuten gültig."
        };
        await client.SendMailAsync(message, cancellationToken);
    }
}

public sealed class IdentityService(
    LearnPipDbContext dbContext,
    IEmailCodeSender sender,
    IConfiguration configuration)
{
    public static string? NormalizeEmail(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var email = input.Trim().ToLowerInvariant();
        return email.Length <= 255 &&
               MailAddress.TryCreate(email, out var parsed) &&
               parsed.Address == email
            ? email : null;
    }

    public async Task<(Guid AccountId, string Secret)> CreatePseudonymousAsync(
        CancellationToken cancellationToken)
    {
        var account = new Account();
        var secret = SessionAuthentication.NewSecret();
        dbContext.Accounts.Add(account);
        dbContext.RecoveryCredentials.Add(new RecoveryCredential
        {
            AccountId = account.Id,
            SecretHash = SessionAuthentication.Hash(secret)
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        return (account.Id, secret);
    }

    public async Task<Guid?> RecoverAsync(string? secret, CancellationToken cancellationToken)
    {
        if (secret is not { Length: 43 } ||
            secret.Any(character => !char.IsAsciiLetterOrDigit(character) &&
                                    character is not ('-' or '_')))
        {
            return null;
        }

        var hash = SessionAuthentication.Hash(secret);
        return await dbContext.RecoveryCredentials.AsNoTracking()
            .Where(credential => credential.SecretHash == hash &&
                                 credential.Account.DeletedAtUtc == null)
            .Select(credential => (Guid?)credential.AccountId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<string> RotateRecoveryAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var secret = SessionAuthentication.NewSecret();
        var credential = await dbContext.RecoveryCredentials
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (credential == null)
        {
            dbContext.RecoveryCredentials.Add(new RecoveryCredential
            {
                AccountId = accountId,
                SecretHash = SessionAuthentication.Hash(secret)
            });
        }
        else
        {
            credential.SecretHash = SessionAuthentication.Hash(secret);
            credential.CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return secret;
    }

    public bool EmailEnabled =>
        sender.IsAvailable && TryGetCodeKey() != null;

    public async Task StartEmailCodeAsync(
        string email, string purpose, Guid? accountId, Guid? sessionId,
        CancellationToken cancellationToken)
    {
        var key = TryGetCodeKey() ?? throw new InvalidOperationException("Email code key is not configured.");
        if (!sender.IsAvailable)
        {
            throw new InvalidOperationException("Mail transport is not configured.");
        }

        var now = DateTimeOffset.UtcNow;
        var recent = await dbContext.EmailLoginCodes.AsNoTracking()
            .Where(item => item.Email == email && item.Purpose == purpose &&
                           item.CreatedAtUtc > now.AddHours(-1))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => item.CreatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);
        // Silent throttling keeps the same response for known and unknown addresses.
        if (recent.Count >= 5 || recent.Count > 0 && recent[0] > now.AddMinutes(-1))
        {
            return;
        }

        await dbContext.EmailLoginCodes
            .Where(item => item.Email == email && item.Purpose == purpose &&
                           item.ConsumedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                item => item.ConsumedAtUtc, now), cancellationToken);

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        var challenge = new EmailLoginCode
        {
            Email = email,
            Purpose = purpose,
            AccountId = accountId,
            InitiatingSessionId = sessionId,
            ExpiresAtUtc = now.AddMinutes(10)
        };
        challenge.CodeHash = HashCode(key, challenge.Id, code);
        dbContext.EmailLoginCodes.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await sender.SendAsync(email, code, cancellationToken);
        }
        catch
        {
            await dbContext.EmailLoginCodes
                .Where(item => item.Id == challenge.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.ConsumedAtUtc, DateTimeOffset.UtcNow), cancellationToken);
            throw;
        }
    }

    public async Task<Guid?> CompleteEmailCodeAsync(
        string email, string code, string purpose, Guid? accountId, Guid? sessionId,
        CancellationToken cancellationToken)
    {
        var key = TryGetCodeKey();
        if (key == null || code.Length != 6 || code.Any(character => character is < '0' or > '9'))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var challenge = await dbContext.EmailLoginCodes.AsNoTracking()
            .Where(item => item.Email == email && item.Purpose == purpose &&
                           item.ConsumedAtUtc == null)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (challenge == null || challenge.ExpiresAtUtc <= now ||
            challenge.FailedAttempts >= 5 ||
            purpose == "link" && (challenge.AccountId != accountId ||
                                  challenge.InitiatingSessionId != sessionId))
        {
            return null;
        }

        var expected = Convert.FromHexString(challenge.CodeHash);
        var actual = Convert.FromHexString(HashCode(key, challenge.Id, code));
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            await dbContext.EmailLoginCodes
                .Where(item => item.Id == challenge.Id && item.ConsumedAtUtc == null &&
                               item.FailedAttempts < 5)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.FailedAttempts, item => item.FailedAttempts + 1), cancellationToken);
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var consumed = await dbContext.EmailLoginCodes
            .Where(item => item.Id == challenge.Id && item.ConsumedAtUtc == null &&
                           item.FailedAttempts < 5 && item.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                item => item.ConsumedAtUtc, now), cancellationToken);
        if (consumed != 1)
        {
            return null;
        }

        var identity = await dbContext.ExternalIdentities
            .SingleOrDefaultAsync(item => item.Provider == "email" && item.Subject == email,
                cancellationToken);
        Guid resolved;
        if (identity != null)
        {
            if (!await dbContext.Accounts.AsNoTracking().AnyAsync(
                    account => account.Id == identity.AccountId && account.DeletedAtUtc == null,
                    cancellationToken))
            {
                return null;
            }

            if (purpose == "link" && identity.AccountId != accountId)
            {
                throw new IdentityConflictException();
            }

            resolved = identity.AccountId;
        }
        else
        {
            if (purpose == "link")
            {
                resolved = accountId!.Value;
            }
            else
            {
                var account = new Account();
                dbContext.Accounts.Add(account);
                resolved = account.Id;
            }

            dbContext.ExternalIdentities.Add(new ExternalIdentity
            {
                AccountId = resolved,
                Provider = "email",
                Subject = email
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new IdentityConflictException();
        }

        return resolved;
    }

    public async Task<Guid> ResolveOidcAsync(
        string issuer, string subject, Guid? linkingAccountId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > 255)
        {
            throw new IdentityConflictException();
        }

        var provider = "oidc:" + SessionAuthentication.Hash(issuer).ToLowerInvariant();
        var identity = await dbContext.ExternalIdentities
            .SingleOrDefaultAsync(item => item.Provider == provider && item.Subject == subject,
                cancellationToken);
        if (identity != null)
        {
            if (!await dbContext.Accounts.AsNoTracking().AnyAsync(
                    account => account.Id == identity.AccountId && account.DeletedAtUtc == null,
                    cancellationToken))
            {
                throw new IdentityConflictException();
            }

            if (linkingAccountId.HasValue && identity.AccountId != linkingAccountId.Value)
            {
                throw new IdentityConflictException();
            }

            return identity.AccountId;
        }

        Guid accountId;
        if (linkingAccountId.HasValue)
        {
            accountId = linkingAccountId.Value;
        }
        else
        {
            var account = new Account();
            dbContext.Accounts.Add(account);
            accountId = account.Id;
        }

        dbContext.ExternalIdentities.Add(new ExternalIdentity
        {
            AccountId = accountId,
            Provider = provider,
            Subject = subject
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when
            (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new IdentityConflictException();
        }

        return accountId;
    }

    private byte[]? TryGetCodeKey()
    {
        var encoded = configuration["Authentication:EmailCodeKey"];
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return null;
        }

        try
        {
            var key = Convert.FromBase64String(encoded);
            return key.Length >= 32 ? key : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string HashCode(byte[] key, Guid id, string code) =>
        Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{id:N}:{code}")));
}
