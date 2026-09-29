namespace LearnPip.Data.Domain;

public sealed class RecoveryCredential
{
    public Guid AccountId { get; set; }
    public string SecretHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Account Account { get; set; } = null!;
}

public sealed class AccountSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Account Account { get; set; } = null!;
}

public sealed class EmailLoginCode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public Guid? AccountId { get; set; }
    public Guid? InitiatingSessionId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public int FailedAttempts { get; set; }
    public Account? Account { get; set; }
    public AccountSession? InitiatingSession { get; set; }
}
