namespace LearnPip.Data.Domain;

public sealed class OfficialCatalogEdition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Revision { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
    public DateOnly ChangedOn { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string QuestionsJson { get; set; } = string.Empty;
}

public sealed class ExamProfileVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AmateurClass { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid CatalogEditionId { get; set; }
    public OfficialCatalogEdition CatalogEdition { get; set; } = null!;
    public string PartsJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ExamSimulation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid ProfileVersionId { get; set; }
    public ExamProfileVersion ProfileVersion { get; set; } = null!;
    public string SnapshotJson { get; set; } = string.Empty;
    public string AnswersJson { get; set; } = "{}";
    public int CurrentPartIndex { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset PartStartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ResultJson { get; set; }
}

public sealed class AccountExamCredit
{
    public Guid AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset ReportedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
