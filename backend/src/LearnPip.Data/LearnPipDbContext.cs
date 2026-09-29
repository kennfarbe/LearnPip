using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data;

public sealed class LearnPipDbContext(DbContextOptions<LearnPipDbContext> options) : DbContext(options)
{
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AdministrationAuditEvent> AdministrationAuditEvents => Set<AdministrationAuditEvent>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountInactivityWarning> AccountInactivityWarnings => Set<AccountInactivityWarning>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<RoleDefinition> Roles => Set<RoleDefinition>();
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();
    public DbSet<PrivateCatalog> PrivateCatalogs => Set<PrivateCatalog>();
    public DbSet<QuestionDraft> QuestionDrafts => Set<QuestionDraft>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<LearningContent> LearningContents => Set<LearningContent>();
    public DbSet<FrequentLearningContent> FrequentLearningContents => Set<FrequentLearningContent>();
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<QuestionContentBlock> QuestionContentBlocks => Set<QuestionContentBlock>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaBlob> MediaBlobs => Set<MediaBlob>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudyAttempt> StudyAttempts => Set<StudyAttempt>();
    public DbSet<StudyAttemptSelection> StudyAttemptSelections => Set<StudyAttemptSelection>();
    public DbSet<ExamObjective> ExamObjectives => Set<ExamObjective>();
    public DbSet<QuestionObjective> QuestionObjectives => Set<QuestionObjective>();
    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<GroupQuestionShare> GroupQuestionShares => Set<GroupQuestionShare>();
    public DbSet<GroupInvitation> GroupInvitations => Set<GroupInvitation>();
    public DbSet<GroupCatalogShare> GroupCatalogShares => Set<GroupCatalogShare>();
    public DbSet<GroupVersionShare> GroupVersionShares => Set<GroupVersionShare>();
    public DbSet<PublicSubmission> PublicSubmissions => Set<PublicSubmission>();
    public DbSet<PublicSubmissionPreview> PublicSubmissionPreviews => Set<PublicSubmissionPreview>();
    public DbSet<PublicSubmissionReview> PublicSubmissionReviews => Set<PublicSubmissionReview>();
    public DbSet<QuestionReport> QuestionReports => Set<QuestionReport>();
    public DbSet<QuestionComment> QuestionComments => Set<QuestionComment>();
    public DbSet<QuestionHelpfulVote> QuestionHelpfulVotes => Set<QuestionHelpfulVote>();
    public DbSet<QuestionModerationEvent> QuestionModerationEvents => Set<QuestionModerationEvent>();
    public DbSet<RecoveryCredential> RecoveryCredentials => Set<RecoveryCredential>();
    public DbSet<AccountSession> AccountSessions => Set<AccountSession>();
    public DbSet<EmailLoginCode> EmailLoginCodes => Set<EmailLoginCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.ToTable(table => table.HasCheckConstraint("CK_QuestionContentBlocks_Owner",
                "(\"QuestionVersionId\" IS NOT NULL AND \"AnswerOptionId\" IS NULL) OR " +
                "(\"QuestionVersionId\" IS NULL AND \"AnswerOptionId\" IS NOT NULL)"));
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
            entity.Property(x => x.AuthorAttribution).HasMaxLength(120).IsRequired();
            entity.Property(x => x.AgeDeclaration).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ReviewNote).HasMaxLength(1000);
            entity.HasOne(x => x.QuestionVersion).WithMany().HasForeignKey(x => x.QuestionVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Account>().WithMany().HasForeignKey(x => x.ReviewedByAccountId)
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
