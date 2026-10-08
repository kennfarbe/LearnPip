// <copyright file="LearnPipDbContext.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data;

/// <summary>
/// Stellt die Datenbanktabellen und das relationale Modell von LearnPip bereit.
/// </summary>
/// <param name="options">Optionen für den Datenbankkontext.</param>
public sealed class LearnPipDbContext(DbContextOptions<LearnPipDbContext> options) : DbContext(options)
{
    /// <summary>Holt die Datenmenge SystemSettings.</summary>
    public DbSet<SystemSetting> SystemSettings => this.Set<SystemSetting>();

    /// <summary>Holt die Datenmenge AdministrationAuditEvents.</summary>
    public DbSet<AdministrationAuditEvent> AdministrationAuditEvents => this.Set<AdministrationAuditEvent>();

    /// <summary>Holt die lokalen Passwortzugänge.</summary>
    public DbSet<PasswordCredential> PasswordCredentials => this.Set<PasswordCredential>();

    /// <summary>Holt die Datenmenge Accounts.</summary>
    public DbSet<Account> Accounts => this.Set<Account>();

    /// <summary>Holt die Datenmenge ReminderPreferences.</summary>
    public DbSet<ReminderPreference> ReminderPreferences => this.Set<ReminderPreference>();

    /// <summary>Holt die Datenmenge FamilyLinks.</summary>
    public DbSet<FamilyLink> FamilyLinks => this.Set<FamilyLink>();

    /// <summary>Holt die Datenmenge FamilyLinkEvents.</summary>
    public DbSet<FamilyLinkEvent> FamilyLinkEvents => this.Set<FamilyLinkEvent>();

    /// <summary>Holt die Datenmenge FamilyGoals.</summary>
    public DbSet<FamilyGoal> FamilyGoals => this.Set<FamilyGoal>();

    /// <summary>Holt die Datenmenge AccountInactivityWarnings.</summary>
    public DbSet<AccountInactivityWarning> AccountInactivityWarnings => this.Set<AccountInactivityWarning>();

    /// <summary>Holt die Datenmenge ExternalIdentities.</summary>
    public DbSet<ExternalIdentity> ExternalIdentities => this.Set<ExternalIdentity>();

    /// <summary>Holt die Datenmenge Roles.</summary>
    public DbSet<RoleDefinition> Roles => this.Set<RoleDefinition>();

    /// <summary>Holt die Datenmenge AccountRoles.</summary>
    public DbSet<AccountRole> AccountRoles => this.Set<AccountRole>();

    /// <summary>Holt die Originalpakete privater Katalogimporte.</summary>
    public DbSet<CatalogPackageImport> CatalogPackageImports => this.Set<CatalogPackageImport>();

    /// <summary>Holt die unveränderten historischen privaten Originalpakete.</summary>
    public DbSet<CatalogPackageImportRevision> CatalogPackageImportRevisions => this.Set<CatalogPackageImportRevision>();

    /// <summary>Holt die optionalen instanzweiten Paketfassungen.</summary>
    public DbSet<InstanceCatalogPackage> InstanceCatalogPackages => this.Set<InstanceCatalogPackage>();

    /// <summary>Holt die fassungsgebundenen Inhaltsrechte.</summary>
    public DbSet<QuestionRights> QuestionRights => this.Set<QuestionRights>();

    /// <summary>Holt die Datenmenge PrivateCatalogs.</summary>
    public DbSet<PrivateCatalog> PrivateCatalogs => this.Set<PrivateCatalog>();

    /// <summary>Holt die Datenmenge QuestionDrafts.</summary>
    public DbSet<QuestionDraft> QuestionDrafts => this.Set<QuestionDraft>();

    /// <summary>Holt die Datenmenge Questions.</summary>
    public DbSet<Question> Questions => this.Set<Question>();

    /// <summary>Holt die Datenmenge LearningContents.</summary>
    public DbSet<LearningContent> LearningContents => this.Set<LearningContent>();

    /// <summary>Holt die Datenmenge FrequentLearningContents.</summary>
    public DbSet<FrequentLearningContent> FrequentLearningContents => this.Set<FrequentLearningContent>();

    /// <summary>Holt die Datenmenge QuestionVersions.</summary>
    public DbSet<QuestionVersion> QuestionVersions => this.Set<QuestionVersion>();

    /// <summary>Holt die Datenmenge QuestionTranslations.</summary>
    public DbSet<QuestionTranslation> QuestionTranslations => this.Set<QuestionTranslation>();

    /// <summary>Holt die Datenmenge TranslationReports.</summary>
    public DbSet<TranslationReport> TranslationReports => this.Set<TranslationReport>();

    /// <summary>Holt die Datenmenge AnswerOptions.</summary>
    public DbSet<AnswerOption> AnswerOptions => this.Set<AnswerOption>();

    /// <summary>Holt die Datenmenge QuestionContentBlocks.</summary>
    public DbSet<QuestionContentBlock> QuestionContentBlocks => this.Set<QuestionContentBlock>();

    /// <summary>Holt die Datenmenge MediaAssets.</summary>
    public DbSet<MediaAsset> MediaAssets => this.Set<MediaAsset>();

    /// <summary>Holt die Datenmenge MediaBlobs.</summary>
    public DbSet<MediaBlob> MediaBlobs => this.Set<MediaBlob>();

    /// <summary>Holt die Datenmenge StudySessions.</summary>
    public DbSet<StudySession> StudySessions => this.Set<StudySession>();

    /// <summary>Holt die Datenmenge StudyAttempts.</summary>
    public DbSet<StudyAttempt> StudyAttempts => this.Set<StudyAttempt>();

    /// <summary>Holt die Datenmenge StudyAttemptSelections.</summary>
    public DbSet<StudyAttemptSelection> StudyAttemptSelections => this.Set<StudyAttemptSelection>();

    /// <summary>Holt die Datenmenge ExamObjectives.</summary>
    public DbSet<ExamObjective> ExamObjectives => this.Set<ExamObjective>();

    /// <summary>Holt die Datenmenge OfficialCatalogEditions.</summary>
    public DbSet<OfficialCatalogEdition> OfficialCatalogEditions => this.Set<OfficialCatalogEdition>();

    /// <summary>Holt die Datenmenge ExamProfileVersions.</summary>
    public DbSet<ExamProfileVersion> ExamProfileVersions => this.Set<ExamProfileVersion>();

    /// <summary>Holt die Datenmenge ExamSimulations.</summary>
    public DbSet<ExamSimulation> ExamSimulations => this.Set<ExamSimulation>();

    /// <summary>Holt die Datenmenge AccountExamCredits.</summary>
    public DbSet<AccountExamCredit> AccountExamCredits => this.Set<AccountExamCredit>();

    /// <summary>Holt die Datenmenge QuestionObjectives.</summary>
    public DbSet<QuestionObjective> QuestionObjectives => this.Set<QuestionObjective>();

    /// <summary>Holt die Datenmenge StudyGroups.</summary>
    public DbSet<StudyGroup> StudyGroups => this.Set<StudyGroup>();

    /// <summary>Holt die Datenmenge GroupMemberships.</summary>
    public DbSet<GroupMembership> GroupMemberships => this.Set<GroupMembership>();

    /// <summary>Holt die Datenmenge GroupQuestionShares.</summary>
    public DbSet<GroupQuestionShare> GroupQuestionShares => this.Set<GroupQuestionShare>();

    /// <summary>Holt die Datenmenge GroupInvitations.</summary>
    public DbSet<GroupInvitation> GroupInvitations => this.Set<GroupInvitation>();

    /// <summary>Holt die Datenmenge GroupCatalogShares.</summary>
    public DbSet<GroupCatalogShare> GroupCatalogShares => this.Set<GroupCatalogShare>();

    /// <summary>Holt die Datenmenge GroupVersionShares.</summary>
    public DbSet<GroupVersionShare> GroupVersionShares => this.Set<GroupVersionShare>();

    /// <summary>Holt die Datenmenge PublicSubmissions.</summary>
    public DbSet<PublicSubmission> PublicSubmissions => this.Set<PublicSubmission>();

    /// <summary>Holt die Datenmenge PublicSubmissionPreviews.</summary>
    public DbSet<PublicSubmissionPreview> PublicSubmissionPreviews => this.Set<PublicSubmissionPreview>();

    /// <summary>Holt die Datenmenge PublicSubmissionReviews.</summary>
    public DbSet<PublicSubmissionReview> PublicSubmissionReviews => this.Set<PublicSubmissionReview>();

    /// <summary>Holt die Datenmenge QuestionReports.</summary>
    public DbSet<QuestionReport> QuestionReports => this.Set<QuestionReport>();

    /// <summary>Holt die Datenmenge QuestionComments.</summary>
    public DbSet<QuestionComment> QuestionComments => this.Set<QuestionComment>();

    /// <summary>Holt die Datenmenge QuestionHelpfulVotes.</summary>
    public DbSet<QuestionHelpfulVote> QuestionHelpfulVotes => this.Set<QuestionHelpfulVote>();

    /// <summary>Holt die Datenmenge QuestionModerationEvents.</summary>
    public DbSet<QuestionModerationEvent> QuestionModerationEvents => this.Set<QuestionModerationEvent>();

    /// <summary>Holt die Datenmenge RecoveryCredentials.</summary>
    public DbSet<RecoveryCredential> RecoveryCredentials => this.Set<RecoveryCredential>();

    /// <summary>Holt die Datenmenge AccountSessions.</summary>
    public DbSet<AccountSession> AccountSessions => this.Set<AccountSession>();

    /// <summary>Holt die Datenmenge EmailLoginCodes.</summary>
    public DbSet<EmailLoginCode> EmailLoginCodes => this.Set<EmailLoginCode>();

    /// <summary>Holt die Datenmenge UserAiCredentials.</summary>
    public DbSet<UserAiCredential> UserAiCredentials => this.Set<UserAiCredential>();

    /// <summary>Holt die Datenmenge AiDailyUsages.</summary>
    public DbSet<AiDailyUsage> AiDailyUsages => this.Set<AiDailyUsage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReminderPreference>(entity =>
        {
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.HasOne<Account>().WithOne().HasForeignKey<ReminderPreference>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<FamilyLink>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InviteHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
            entity.Property(x => x.VerificationReference).HasMaxLength(120);
            entity.HasIndex(x => x.InviteHash).IsUnique();
            entity.HasIndex(x => new { x.ChildAccountId, x.ParentAccountId, x.Status });
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ChildAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.VerifiedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.RevokedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FamilyLinkEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(24).IsRequired();
            entity.HasIndex(x => new { x.FamilyLinkId, x.CreatedAtUtc });
            entity.HasOne<FamilyLink>().WithMany().HasForeignKey(x => x.FamilyLinkId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<FamilyGoal>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.HasIndex(x => x.FamilyLinkId);
            entity.HasOne<FamilyLink>().WithMany().HasForeignKey(x => x.FamilyLinkId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<QuestionTranslation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Language).HasMaxLength(8).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
            entity.Property(x => x.PayloadJson).HasMaxLength(65536).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(500).IsRequired();
            entity.Property(x => x.License).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Provenance).HasMaxLength(24).IsRequired();
            entity.HasIndex(x => new { x.QuestionVersionId, x.Language, x.Revision }).IsUnique();
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TranslationReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Details).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.QuestionTranslationId, x.CreatedAtUtc });
            entity.HasOne(x => x.Translation).WithMany().HasForeignKey(x => x.QuestionTranslationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<UserAiCredential>(entity =>
        {
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.Ciphertext).IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AiDailyUsage>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.Day, x.Mode });
            entity.Property(x => x.Mode).HasMaxLength(32).IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(80);
            entity.Property(x => x.Value).HasMaxLength(1000).IsRequired();
        });
        modelBuilder.Entity<AdministrationAuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Target).HasMaxLength(160).IsRequired();
            entity.Property(x => x.PreviousValue).HasMaxLength(1000);
            entity.Property(x => x.NewValue).HasMaxLength(1000);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ActorAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AgeBand).HasMaxLength(8).HasDefaultValue("unknown").IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.LastActivityAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(x => x.LastActivityAtUtc);
        });

        modelBuilder.Entity<AccountInactivityWarning>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.PhaseDays, x.ActivityAtUtc });
            entity.Property(x => x.DeliveryStatus).HasMaxLength(16).IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExternalIdentity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(255).IsRequired();
            entity.HasIndex(x => new { x.Provider, x.Subject }).IsUnique();
            entity.HasOne(x => x.Account).WithMany(x => x.ExternalIdentities)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PasswordCredential>(entity =>
        {
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.Username).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasOne(x => x.Account).WithOne()
                .HasForeignKey<PasswordCredential>(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecoveryCredential>(entity =>
        {
            entity.HasKey(x => x.AccountId);
            entity.Property(x => x.SecretHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.SecretHash).IsUnique();
            entity.HasOne(x => x.Account).WithOne()
                .HasForeignKey<RecoveryCredential>(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.AccountId, x.ExpiresAtUtc });
            entity.HasOne(x => x.Account).WithMany()
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EmailLoginCode>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Purpose).HasMaxLength(16).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.Email, x.Purpose, x.CreatedAtUtc });
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.InitiatingSession).WithMany()
                .HasForeignKey(x => x.InitiatingSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleDefinition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Scope).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.Scope, x.Code }).IsUnique();
        });

        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.RoleDefinitionId });
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RoleDefinition).WithMany(x => x.AccountRoles)
                .HasForeignKey(x => x.RoleDefinitionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogPackageImportRevision>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PackageId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.QuestionIdsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.QuestionVersionIdsJson).HasColumnType("jsonb").IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<InstanceCatalogPackage>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PackageId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CatalogVersion).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ArchiveSha256).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.PackageId, x.CatalogVersion }).IsUnique();
        });
        modelBuilder.Entity<QuestionRights>(entity =>
        {
            entity.HasKey(x => x.QuestionId);
            entity.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.HistoryJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("[]");
            entity.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CatalogPackageImport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PackageId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CatalogVersion).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.QuestionIdsJson).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.QuestionVersionIdsJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => new { x.OwnerAccountId, x.PackageId }).IsUnique();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PrivateCatalog>().WithMany().HasForeignKey(x => x.PrivateCatalogId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PrivateCatalog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.OwnerAccountId, x.Name }).IsUnique();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionDraft>(entity =>
        {
            entity.HasKey(x => x.QuestionId);
            entity.Property(x => x.PayloadJson).HasMaxLength(65536).IsRequired();
            entity.HasOne(x => x.Question).WithOne(x => x.Draft)
                .HasForeignKey<QuestionDraft>(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningContent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.OwnerAccountId);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FrequentLearningContent>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.LearningContentId });
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.LearningContent).WithMany()
                .HasForeignKey(x => x.LearningContentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OwnerAccountId, x.UpdatedAtUtc });
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(x => x.Owner).WithMany(x => x.Questions)
                .HasForeignKey(x => x.OwnerAccountId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LearningContent).WithMany(x => x.Questions)
                .HasForeignKey(x => x.LearningContentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PrivateCatalog).WithMany(x => x.Questions)
                .HasForeignKey(x => x.PrivateCatalogId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<QuestionVersion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Visibility).HasMaxLength(16).IsRequired().HasDefaultValue("private");
            entity.Property(x => x.Prompt).HasMaxLength(12000).IsRequired();
            entity.Property(x => x.Explanation).HasMaxLength(12000);
            entity.Property(x => x.SelectionMode).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Topic).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Language).HasMaxLength(35).IsRequired();
            entity.Property(x => x.Source).HasMaxLength(500).IsRequired();
            entity.Property(x => x.License).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AuthorAttribution).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => new { x.QuestionId, x.VersionNumber }).IsUnique();
            entity.HasOne(x => x.Question).WithMany(x => x.Versions)
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AnswerOption>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).HasMaxLength(4000).IsRequired();
            entity.HasIndex(x => new { x.QuestionVersionId, x.SortOrder }).IsUnique();
            entity.HasOne(x => x.QuestionVersion).WithMany(x => x.AnswerOptions)
                .HasForeignKey(x => x.QuestionVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionContentBlock>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Section).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Kind).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Text).HasMaxLength(4000);
            entity.HasIndex(x => new { x.QuestionVersionId, x.Section, x.SortOrder })
                .IsUnique().HasFilter("\"QuestionVersionId\" IS NOT NULL");
            entity.HasIndex(x => new { x.AnswerOptionId, x.SortOrder })
                .IsUnique().HasFilter("\"AnswerOptionId\" IS NOT NULL");
            entity.HasOne(x => x.QuestionVersion).WithMany(x => x.Blocks)
                .HasForeignKey(x => x.QuestionVersionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AnswerOption).WithMany(x => x.Blocks)
                .HasForeignKey(x => x.AnswerOptionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.MediaAsset).WithMany()
                .HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Restrict);
            const string questionContentOwnerConstraint =
                "(\"QuestionVersionId\" IS NOT NULL AND \"AnswerOptionId\" IS NULL) OR " +
                "(\"QuestionVersionId\" IS NULL AND \"AnswerOptionId\" IS NOT NULL)";
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_QuestionContentBlocks_Owner",
                questionContentOwnerConstraint));
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AltText).HasMaxLength(300).IsRequired();
            entity.HasIndex(x => x.StorageKey).IsUnique();
            entity.HasIndex(x => new { x.OwnerAccountId, x.CreatedAtUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_MediaAssets_ByteLength", "\"ByteLength\" > 0"));
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QuestionVersion).WithMany(x => x.MediaAssets)
                .HasForeignKey(x => x.QuestionVersionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MediaBlob>(entity =>
        {
            entity.HasKey(x => x.MediaAssetId);
            entity.HasOne(x => x.MediaAsset).WithOne()
                .HasForeignKey<MediaBlob>(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PlanJson).HasMaxLength(8192);
            entity.HasIndex(x => new { x.AccountId, x.StartedAtUtc });
            entity.HasOne(x => x.Account).WithMany(x => x.StudySessions)
                .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudyAttempt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.StudySessionId, x.AnsweredAtUtc });
            entity.HasOne(x => x.StudySession).WithMany(x => x.Attempts)
                .HasForeignKey(x => x.StudySessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudyAttemptSelection>(entity =>
        {
            entity.HasKey(x => new { x.StudyAttemptId, x.AnswerOptionId });
            entity.HasOne(x => x.StudyAttempt).WithMany(x => x.Selections)
                .HasForeignKey(x => x.StudyAttemptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AnswerOption).WithMany()
                .HasForeignKey(x => x.AnswerOptionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OfficialCatalogEdition>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Revision).HasMaxLength(80).IsRequired();
            entity.Property(x => x.SourceUrl).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.License).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Attribution).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => new { x.Code, x.Revision }).IsUnique();
        });
        modelBuilder.Entity<ExamProfileVersion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.AmateurClass).HasMaxLength(1).IsRequired();
            entity.HasIndex(x => new { x.Code, x.Version }).IsUnique();
            entity.HasOne(x => x.CatalogEdition).WithMany()
                .HasForeignKey(x => x.CatalogEditionId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<ExamSimulation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.AccountId, x.StartedAtUtc });
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ProfileVersion).WithMany()
                .HasForeignKey(x => x.ProfileVersionId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<AccountExamCredit>(entity =>
        {
            entity.HasKey(x => new { x.AccountId, x.Code });
            entity.Property(x => x.Code).HasMaxLength(16).IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamObjective>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000);
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<QuestionObjective>(entity =>
        {
            entity.HasKey(x => new { x.QuestionId, x.ExamObjectiveId });
            entity.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ExamObjective).WithMany(x => x.QuestionObjectives)
                .HasForeignKey(x => x.ExamObjectiveId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudyGroup>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupMembership>(entity =>
        {
            entity.HasKey(x => new { x.StudyGroupId, x.AccountId });
            entity.HasOne(x => x.StudyGroup).WithMany(x => x.Memberships)
                .HasForeignKey(x => x.StudyGroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RoleDefinition).WithMany().HasForeignKey(x => x.RoleDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupQuestionShare>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.StudyGroupId, x.QuestionId })
                .IsUnique()
                .HasFilter("\"RevokedAtUtc\" IS NULL");
            entity.HasOne(x => x.StudyGroup).WithMany().HasForeignKey(x => x.StudyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.SharedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupInvitation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.CodeHash).IsUnique();
            entity.HasOne(x => x.StudyGroup).WithMany().HasForeignKey(x => x.StudyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupCatalogShare>(entity =>
        {
            entity.HasKey(x => new { x.StudyGroupId, x.PrivateCatalogId });
            entity.HasOne(x => x.StudyGroup).WithMany().HasForeignKey(x => x.StudyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PrivateCatalog).WithMany().HasForeignKey(x => x.PrivateCatalogId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.SharedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GroupVersionShare>(entity =>
        {
            entity.HasKey(x => new { x.StudyGroupId, x.QuestionVersionId });
            entity.HasOne(x => x.StudyGroup).WithMany().HasForeignKey(x => x.StudyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PrivateCatalog).WithMany().HasForeignKey(x => x.PrivateCatalogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PublicSubmission>(entity =>
        {
            entity.HasKey(x => x.QuestionVersionId);
            entity.Property(x => x.Status).HasMaxLength(24).IsRequired();
            entity.Property(x => x.LicenseChoice).HasMaxLength(40).IsRequired();
            entity.Property(x => x.RightsJson).HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
            entity.Property(x => x.AuthorAttribution).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AgeDeclaration).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ReviewNote).HasMaxLength(1000);
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ReviewedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.GuardianApprovedByAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.Status, x.SubmittedAtUtc });
        });

        modelBuilder.Entity<PublicSubmissionPreview>(entity =>
        {
            entity.HasKey(x => x.QuestionVersionId);
            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
            entity.HasOne<QuestionVersion>().WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PublicSubmissionReview>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Decision).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000).IsRequired();
            entity.HasOne<QuestionVersion>().WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ModeratorAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.QuestionVersionId);
        });

        modelBuilder.Entity<QuestionReport>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reason).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(16).IsRequired();
            entity.HasIndex(x => new { x.Status, x.QuestionVersionId });
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<QuestionComment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            entity.HasIndex(x => new { x.QuestionVersionId, x.CreatedAtUtc });
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<QuestionHelpfulVote>(entity =>
        {
            entity.HasKey(x => new { x.QuestionVersionId, x.AccountId });
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<QuestionModerationEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Note).HasMaxLength(1000).IsRequired();
            entity.HasIndex(x => new { x.QuestionVersionId, x.CreatedAtUtc });
            entity.HasOne<QuestionVersion>().WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ModeratorAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        base.OnModelCreating(modelBuilder);
    }
}
