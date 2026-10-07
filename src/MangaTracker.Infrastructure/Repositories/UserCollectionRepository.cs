using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class UserCollectionRepository : IUserCollectionRepository
{
    private readonly MangaTrackerDbContext _dbContext;

    public UserCollectionRepository(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserCollection?> GetByUserIdAndEditionIdAsync(
        Guid userId,
        Guid editionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.UserCollections
            .FirstOrDefaultAsync(
                collection =>
                    collection.UserId == userId &&
                    collection.EditionId == editionId,
                cancellationToken);
    }

    public Task<UserCollection?> GetByUserIdAndIdAsync(
        Guid userId,
        Guid collectionId,
        CancellationToken cancellationToken = default)
    {
        // Tomes and OwnedTomes are sibling collections: in a single query SQL would return
        // their cartesian product (100 tomes, all owned = 10,000 rows). Split queries load
        // each collection on its own.
        return _dbContext.UserCollections
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Series)
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Tomes)
            .Include(collection => collection.OwnedTomes)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                collection =>
                    collection.UserId == userId &&
                    collection.Id == collectionId,
                cancellationToken);
    }

    public async Task AddAsync(
        UserCollection userCollection,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UserCollections.AddAsync(userCollection, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserCollection>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Used by the sync, which only needs each edition and its tomes; owned tomes and
        // series are not loaded.
        return await _dbContext.UserCollections
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Tomes)
            .Where(collection => collection.UserId == userId)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public void Remove(UserCollection userCollection)
    {
        _dbContext.UserCollections.Remove(userCollection);
    }
}