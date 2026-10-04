using MangaTracker.Application.Collections.GetPendingTomes;
using MangaTracker.Application.Collections.GetUserCollections;

namespace MangaTracker.Application.Abstractions;

// Read side for screens that only display data. Repositories load aggregates to change
// them; these queries project straight to the shape the API returns, computing counts
// in the database instead of loading every edition, tome and owned tome into memory.
public interface ICollectionQueries
{
    Task<IReadOnlyCollection<UserCollectionListItemResult>> GetCollectionSummariesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PendingTomeResult>> GetPendingTomesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
