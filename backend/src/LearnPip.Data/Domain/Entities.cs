namespace LearnPip.Data.Domain;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];
    public ICollection<Question> Questions { get; set; } = [];
    public ICollection<StudySession> StudySessions { get; set; } = [];
}

public sealed class ExternalIdentity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Account Account { get; set; } = null!;
}

public sealed class RoleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Scope { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<AccountRole> AccountRoles { get; set; } = [];
}

public sealed class AccountRole
{
    public Guid AccountId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public DateTimeOffset GrantedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Account Account { get; set; } = null!;
    public RoleDefinition RoleDefinition { get; set; } = null!;
}

public sealed class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerAccountId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Account Owner { get; set; } = null!;
    public ICollection<QuestionVersion> Versions { get; set; } = [];
}

public sealed class QuestionVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public int VersionNumber { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string SelectionMode { get; set; } = "single";
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Language { get; set; } = "de";
    public string Source { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public DateTimeOffset PublishedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Question Question { get; set; } = null!;
    public Account CreatedBy { get; set; } = null!;
    public ICollection<AnswerOption> AnswerOptions { get; set; } = [];
    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
}

public sealed class AnswerOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionVersionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
    public QuestionVersion QuestionVersion { get; set; } = null!;
    public ICollection<QuestionContentBlock> Blocks { get; set; } = [];
}

public sealed class QuestionContentBlock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? QuestionVersionId { get; set; }
    public Guid? AnswerOptionId { get; set; }
    public string Section { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string? Text { get; set; }
    public Guid? MediaAssetId { get; set; }
    public QuestionVersion? QuestionVersion { get; set; }
    public AnswerOption? AnswerOption { get; set; }
    public MediaAsset? MediaAsset { get; set; }
}

public sealed class MediaAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerAccountId { get; set; }
    public Guid? QuestionVersionId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string MediaType { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public long ByteLength { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Account Owner { get; set; } = null!;
    public QuestionVersion? QuestionVersion { get; set; }
}

public sealed class StudySession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Account Account { get; set; } = null!;
    public ICollection<StudyAttempt> Attempts { get; set; } = [];
}

public sealed class StudyAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudySessionId { get; set; }
    public Guid QuestionVersionId { get; set; }
    public bool IsCorrect { get; set; }
    public DateTimeOffset AnsweredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public StudySession StudySession { get; set; } = null!;
    public QuestionVersion QuestionVersion { get; set; } = null!;
    public ICollection<StudyAttemptSelection> Selections { get; set; } = [];
}

public sealed class StudyAttemptSelection
{
    public Guid StudyAttemptId { get; set; }
    public Guid AnswerOptionId { get; set; }
    public StudyAttempt StudyAttempt { get; set; } = null!;
    public AnswerOption AnswerOption { get; set; } = null!;
}

public sealed class ExamObjective
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<QuestionObjective> QuestionObjectives { get; set; } = [];
}

public sealed class QuestionObjective
{
    public Guid QuestionId { get; set; }
    public Guid ExamObjectiveId { get; set; }
    public Question Question { get; set; } = null!;
    public ExamObjective ExamObjective { get; set; } = null!;
}

public sealed class StudyGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerAccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public ICollection<GroupMembership> Memberships { get; set; } = [];
}

public sealed class GroupMembership
{
    public Guid StudyGroupId { get; set; }
    public Guid AccountId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public StudyGroup StudyGroup { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public RoleDefinition RoleDefinition { get; set; } = null!;
}

public sealed class GroupQuestionShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudyGroupId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid SharedByAccountId { get; set; }
    public DateTimeOffset SharedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public StudyGroup StudyGroup { get; set; } = null!;
    public Question Question { get; set; } = null!;
}

public sealed class MediaBlob
{
    public Guid MediaAssetId { get; set; }
    public byte[] Data { get; set; } = [];
    public MediaAsset MediaAsset { get; set; } = null!;
}
