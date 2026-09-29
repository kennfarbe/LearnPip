using LearnPip.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace LearnPip.Data.Services;

public sealed class PrivateMediaCatalog(LearnPipDbContext dbContext)
{
    public Task<List<MediaAsset>> ListOwnedByAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        dbContext.MediaAssets
            .AsNoTracking()
            .Where(asset => asset.OwnerAccountId == accountId && asset.DeletedAtUtc == null)
            .OrderByDescending(asset => asset.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
