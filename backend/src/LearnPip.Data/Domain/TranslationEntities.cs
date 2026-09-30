namespace LearnPip.Data.Domain;

public sealed class QuestionTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionVersionId { get; set; }
    public string Language { get; set; } = string.Empty;
    public int Revision { get; set; }
    public string Status { get; set; } = "draft";
    public string PayloadJson { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Provenance { get; set; } = "manual";
    public Guid CreatedByAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public QuestionVersion QuestionVersion { get; set; } = null!;
}

public sealed class TranslationReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionTranslationId { get; set; }
    public Guid AccountId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public QuestionTranslation Translation { get; set; } = null!;
}
