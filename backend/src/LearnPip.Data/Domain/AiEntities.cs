namespace LearnPip.Data.Domain;

public sealed class UserAiCredential
{
    public Guid AccountId { get; set; }
    public string Ciphertext { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class AiDailyUsage
{
    public Guid AccountId { get; set; }
    public DateOnly Day { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int UsedRequests { get; set; }
}
