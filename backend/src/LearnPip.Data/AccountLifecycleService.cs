using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data;

public interface IInactivityNoticeSender
{
    bool IsAvailable { get; }
    Task SendAsync(string email, int phaseDays, DateTimeOffset lastActivityAtUtc,
        CancellationToken cancellationToken);
}

public sealed record LifecycleRunResult(int WarningsClaimed, int Deactivated, int Deleted);

public sealed class AccountLifecycleService(LearnPipDbContext db, IInactivityNoticeSender sender)
{
    public async Task<LifecycleRunResult> RunOnceAsync(DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var ids = await db.Accounts.AsNoTracking()
            .Where(account => account.DeletedAtUtc == null &&
                (account.LastActivityAtUtc <= now.AddDays(-60) || account.DisabledAtUtc != null))
            .Select(account => account.Id).ToListAsync(cancellationToken);
        var warnings = 0;
        var deactivated = 0;
        var deleted = 0;
        foreach (var id in ids)
        {
            db.ChangeTracker.Clear();
            var action = await ProcessAsync(id, now, cancellationToken);
            if (action == "warned") warnings++;
            if (action == "deactivated") deactivated++;
            if (action == "deleted") deleted++;
        }
        return new LifecycleRunResult(warnings, deactivated, deleted);
    }

    private async Task<string> ProcessAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(1049071810)", cancellationToken);
        var account = await LockedAccount(id, cancellationToken);
        if (account == null || account.DeletedAtUtc != null || await IsPrivileged(id, cancellationToken))
            return "unchanged";

        if (account.DisabledAtUtc.HasValue)
        {
            // Require both thresholds, even if a previous job deactivated the account late.
            if (account.DisabledAtUtc.Value.AddDays(90) > now ||
                account.LastActivityAtUtc.AddDays(180) > now) return "unchanged";
            await DeleteAccountAsync(id, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return "deleted";
        }

        if (account.LastActivityAtUtc.AddDays(90) <= now)
        {
            account.DisabledAtUtc = now;
            await db.AccountSessions.Where(session => session.AccountId == id &&
                    session.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RevokedAtUtc, now),
                    cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return "deactivated";
        }

        var days = (now - account.LastActivityAtUtc).TotalDays;
        var phase = days >= 87 ? 87 : days >= 76 ? 76 : days >= 60 ? 60 : 0;
        if (phase == 0 || !sender.IsAvailable) return "unchanged";
        var email = await db.ExternalIdentities.AsNoTracking()
            .Where(identity => identity.AccountId == id && identity.Provider == "email")
            .Select(identity => identity.Subject).FirstOrDefaultAsync(cancellationToken);
        if (email == null || await db.AccountInactivityWarnings.AnyAsync(item =>
                item.AccountId == id && item.PhaseDays == phase &&
                item.ActivityAtUtc == account.LastActivityAtUtc, cancellationToken))
            return "unchanged";

        // Commit the one-time claim before SMTP. A crash or SMTP failure cannot
        // cause a duplicate warning on the next run; delivery is recorded separately.
        var warning = new AccountInactivityWarning
        {
            AccountId = id,
            PhaseDays = phase,
            ActivityAtUtc = account.LastActivityAtUtc,
            ClaimedAtUtc = now
        };
        db.AccountInactivityWarnings.Add(warning);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        db.Entry(account).State = EntityState.Detached;

        await using var sendTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var current = await LockedAccount(id, cancellationToken);
        if (current == null || current.DeletedAtUtc != null || current.DisabledAtUtc != null ||
            current.LastActivityAtUtc != warning.ActivityAtUtc ||
            await IsPrivileged(id, cancellationToken))
        {
            warning.DeliveryStatus = "cancelled";
        }
        else
        {
            try
            {
                await sender.SendAsync(email, phase, current.LastActivityAtUtc, cancellationToken);
                warning.DeliveryStatus = "sent";
                warning.SentAtUtc = now;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                warning.DeliveryStatus = "failed";
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await sendTransaction.CommitAsync(cancellationToken);
        return "warned";
    }

    private async Task<Account?> LockedAccount(Guid id, CancellationToken cancellationToken) =>
        await db.Accounts.FromSqlInterpolated(
            $"SELECT * FROM \"Accounts\" WHERE \"Id\" = {id} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<bool> IsPrivileged(Guid id, CancellationToken cancellationToken) =>
        db.AccountRoles.AnyAsync(role => role.AccountId == id &&
            role.RoleDefinition.Scope == "system" &&
            (role.RoleDefinition.Code == "moderator" || role.RoleDefinition.Code == "admin"),
            cancellationToken);

    private async Task DeleteAccountAsync(Guid id, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var emails = await db.ExternalIdentities.AsNoTracking().Where(item =>
            item.AccountId == id && item.Provider == "email")
            .Select(item => item.Subject).ToListAsync(cancellationToken);
        var groupIds = await db.StudyGroups.AsNoTracking().Where(item => item.OwnerAccountId == id)
            .Select(item => item.Id).ToListAsync(cancellationToken);
        var idText = id.ToString();

        await db.AdministrationAuditEvents.Where(item => item.ActorAccountId == id ||
            item.Target.Contains(idText)).ExecuteDeleteAsync(cancellationToken);
        foreach (var groupId in groupIds)
        {
            var groupText = groupId.ToString();
            await db.AdministrationAuditEvents.Where(item => item.Target.Contains(groupText))
                .ExecuteDeleteAsync(cancellationToken);
        }
        await db.GroupQuestionShares.Where(item => item.SharedByAccountId == id ||
            item.Question.OwnerAccountId == id || item.StudyGroup.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.GroupInvitations.Where(item => item.CreatedByAccountId == id ||
            item.StudyGroup.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.GroupCatalogShares.Where(item => item.SharedByAccountId == id ||
            item.PrivateCatalog.OwnerAccountId == id || item.StudyGroup.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.GroupVersionShares.Where(item => item.QuestionVersion.Question.OwnerAccountId == id ||
            item.PrivateCatalog.OwnerAccountId == id || item.StudyGroup.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.GroupMemberships.Where(item => item.AccountId == id ||
            item.StudyGroup.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.StudyAttemptSelections.Where(item => item.StudyAttempt.StudySession.AccountId == id ||
            item.StudyAttempt.QuestionVersion.Question.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.StudyAttempts.Where(item => item.StudySession.AccountId == id ||
            item.QuestionVersion.Question.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.StudySessions.Where(item => item.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.QuestionObjectives.Where(item => item.Question.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.QuestionContentBlocks.Where(item =>
            item.QuestionVersion != null && item.QuestionVersion.Question.OwnerAccountId == id ||
            item.AnswerOption != null && item.AnswerOption.QuestionVersion.Question.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.MediaBlobs.Where(item => item.MediaAsset.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.MediaAssets.Where(item => item.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.AnswerOptions.Where(item => item.QuestionVersion.Question.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.QuestionVersions.Where(item => item.Question.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.QuestionDrafts.Where(item => item.Question.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.Questions.Where(item => item.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.FrequentLearningContents.Where(item => item.AccountId == id ||
            item.LearningContent.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.LearningContents.Where(item => item.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.PrivateCatalogs.Where(item => item.OwnerAccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        await db.StudyGroups.Where(item => item.OwnerAccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.AccountRoles.Where(item => item.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.EmailLoginCodes.Where(item => item.AccountId == id ||
            item.InitiatingSession != null && item.InitiatingSession.AccountId == id ||
            emails.Contains(item.Email)).ExecuteDeleteAsync(cancellationToken);
        await db.ExternalIdentities.Where(item => item.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.RecoveryCredentials.Where(item => item.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.AccountSessions.Where(item => item.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await db.AccountInactivityWarnings.Where(item => item.AccountId == id)
            .ExecuteDeleteAsync(cancellationToken);
        var removed = await db.Accounts.Where(item => item.Id == id &&
            item.DisabledAtUtc <= now.AddDays(-90) &&
            item.LastActivityAtUtc <= now.AddDays(-180))
            .ExecuteDeleteAsync(cancellationToken);
        if (removed != 1) throw new InvalidOperationException("Account activity changed during deletion.");
    }
}
