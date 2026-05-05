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
        return _dbContext.UserCollections
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Series)
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Tomes)
            .Include(collection => collection.OwnedTomes)
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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserCollection>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserCollections
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Series)
            .Include(collection => collection.Edition)
                .ThenInclude(edition => edition.Tomes)
            .Include(collection => collection.OwnedTomes)
            .Where(collection => collection.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}