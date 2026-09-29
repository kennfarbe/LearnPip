namespace LearnPip.Data.Domain;

public sealed class PrivateCatalog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Question> Questions { get; set; } = [];
}

public sealed class QuestionDraft
{
    public Guid QuestionId { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Question Question { get; set; } = null!;
}
