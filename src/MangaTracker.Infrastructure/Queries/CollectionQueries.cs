using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Collections.GetPendingTomes;
using MangaTracker.Application.Collections.GetUserCollections;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Queries;

public sealed class CollectionQueries : ICollectionQueries
{
    private readonly MangaTrackerDbContext _dbContext;

    public CollectionQueries(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<UserCollectionListItemResult>> GetCollectionSummariesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // One SQL query: tome counts are subqueries, nothing is tracked.
        return await _dbContext.UserCollections
            .AsNoTracking()
            .Where(collection => collection.UserId == userId)
            .OrderBy(collection => collection.Edition.Series.Title)
            .ThenBy(collection => collection.Edition.PublisherName)
            .Select(collection => new
            {
                collection.Id,
                collection.EditionId,
                collection.Edition.ComicVineVolumeId,
                collection.Edition.ComicVineApiDetailUrl,
                collection.Edition.Series.Title,
                collection.Edition.PublisherName,
                collection.Edition.ImageUrl,
                TotalTomes = collection.Edition.Tomes.Count,
                OwnedTomes = collection.OwnedTomes.Count
            })
            .Select(row => new UserCollectionListItemResult(
                row.Id,
                row.EditionId,
                row.ComicVineVolumeId,
                row.ComicVineApiDetailUrl,
                row.Title,
                row.PublisherName,
                row.ImageUrl,
                row.TotalTomes,
                row.OwnedTomes,
                row.TotalTomes - row.OwnedTomes))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PendingTomeResult>> GetPendingTomesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Pending = tomes of the user's editions with no matching owned row (NOT EXISTS).
        return await _dbContext.UserCollections
            .AsNoTracking()
            .Where(collection => collection.UserId == userId)
            .SelectMany(
                collection => collection.Edition.Tomes,
                (collection, tome) => new { collection, tome })
            .Where(row => !row.collection.OwnedTomes.Any(owned => owned.TomeId == row.tome.Id))
            .OrderBy(row => row.collection.Edition.Series.Title)
            .ThenBy(row => row.collection.Edition.PublisherName)
            .ThenBy(row => row.tome.NormalizedNumber ?? int.MaxValue)
            .ThenBy(row => row.tome.IssueNumber)
            .Select(row => new PendingTomeResult(
                row.collection.Id,
                row.collection.EditionId,
                row.tome.Id,
                row.collection.Edition.Series.Title,
                row.collection.Edition.Name,
                row.collection.Edition.PublisherName,
                row.tome.IssueNumber,
                row.tome.NormalizedNumber,
                row.tome.Title,
                row.tome.ImageUrl,
                row.tome.CoverDate,
                row.tome.StoreDate))
            .ToListAsync(cancellationToken);
    }
}
