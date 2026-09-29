using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data;

public sealed class LearnPipDbContext(DbContextOptions<LearnPipDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();
    public DbSet<RoleDefinition> Roles => Set<RoleDefinition>();
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudyAttempt> StudyAttempts => Set<StudyAttempt>();
    public DbSet<ExamObjective> ExamObjectives => Set<ExamObjective>();
    public DbSet<QuestionObjective> QuestionObjectives => Set<QuestionObjective>();
    public DbSet<StudyGroup> StudyGroups => Set<StudyGroup>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<GroupQuestionShare> GroupQuestionShares => Set<GroupQuestionShare>();
    public DbSet<RecoveryCredential> RecoveryCredentials => Set<RecoveryCredential>();
    public DbSet<AccountSession> AccountSessions => Set<AccountSession>();
    public DbSet<EmailLoginCode> EmailLoginCodes => Set<EmailLoginCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
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

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.OwnerAccountId, x.UpdatedAtUtc });
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.UpdatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasOne(x => x.Owner).WithMany(x => x.Questions)
                .HasForeignKey(x => x.OwnerAccountId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionVersion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Prompt).HasMaxLength(12000).IsRequired();
            entity.Property(x => x.Explanation).HasMaxLength(12000);
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

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.StorageKey).IsUnique();
            entity.HasIndex(x => new { x.OwnerAccountId, x.CreatedAtUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_MediaAssets_ByteLength", "\"ByteLength\" > 0"));
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.QuestionVersion).WithMany(x => x.MediaAssets)
                .HasForeignKey(x => x.QuestionVersionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(x => x.Id);
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

        base.OnModelCreating(modelBuilder);
    }
}
