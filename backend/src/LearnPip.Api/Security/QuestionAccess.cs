using LearnPip.Data;
using LearnPip.Data.Domain;

namespace LearnPip.Api.Security;

public static class QuestionAccess
{
    public static IQueryable<QuestionVersion> GroupVersions(LearnPipDbContext db, Guid groupId) =>
        db.QuestionVersions.Where(version => version.Question.DeletedAtUtc == null &&
            (db.GroupVersionShares.Any(share => share.QuestionVersionId == version.Id &&
                share.StudyGroupId == groupId && share.StudyGroup.DeletedAtUtc == null &&
                db.GroupCatalogShares.Any(catalog => catalog.StudyGroupId == groupId &&
                    catalog.PrivateCatalogId == share.PrivateCatalogId)) ||
             db.GroupQuestionShares.Any(share => share.QuestionId == version.QuestionId &&
                share.StudyGroupId == groupId && share.RevokedAtUtc == null &&
                share.StudyGroup.DeletedAtUtc == null &&
                version.PublishedAtUtc <= share.SharedAtUtc)));

    public static IQueryable<QuestionVersion> ReadableVersions(LearnPipDbContext db, Guid accountId) =>
        db.QuestionVersions.Where(version => version.Question.DeletedAtUtc == null &&
            (version.Question.OwnerAccountId == accountId ||
             version.Visibility == "public" && db.PublicSubmissions.Any(submission =>
                 submission.QuestionVersionId == version.Id && submission.Status == "approved") ||
             db.GroupVersionShares.Any(share => share.QuestionVersionId == version.Id &&
                 share.StudyGroup.DeletedAtUtc == null &&
                 (share.StudyGroup.OwnerAccountId == accountId ||
                  share.StudyGroup.Memberships.Any(member => member.AccountId == accountId)) &&
                 db.GroupCatalogShares.Any(catalog => catalog.StudyGroupId == share.StudyGroupId &&
                     catalog.PrivateCatalogId == share.PrivateCatalogId)) ||
             db.GroupQuestionShares.Any(share => share.QuestionId == version.QuestionId &&
                 share.RevokedAtUtc == null && share.StudyGroup.DeletedAtUtc == null &&
                 version.PublishedAtUtc <= share.SharedAtUtc &&
                 (share.StudyGroup.OwnerAccountId == accountId ||
                  share.StudyGroup.Memberships.Any(member => member.AccountId == accountId)))));

    public static IQueryable<QuestionVersion> PublicVersions(LearnPipDbContext db) =>
        db.QuestionVersions.Where(version => version.Visibility == "public" &&
            db.PublicSubmissions.Any(submission => submission.QuestionVersionId == version.Id &&
                submission.Status == "approved") &&
            version.Question.DeletedAtUtc == null);
}
