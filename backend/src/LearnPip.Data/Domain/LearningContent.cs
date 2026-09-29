namespace LearnPip.Data.Domain;

public sealed class LearningContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerAccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ICollection<Question> Questions { get; set; } = [];
}

public sealed class FrequentLearningContent
{
    public Guid AccountId { get; set; }
    public Guid LearningContentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public LearningContent LearningContent { get; set; } = null!;
}
