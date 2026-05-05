using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class MangaCollectionRepository : IMangaCollectionRepository
{
    private readonly MangaTrackerDbContext _dbContext;

    public MangaCollectionRepository(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<MangaCollectionItem>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.MangaCollectionItems
            .AsNoTracking()
            .Where(manga => manga.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public Task<MangaCollectionItem?> GetByUserIdAndMalIdAsync(
        Guid userId,
        int malId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MangaCollectionItems
            .FirstOrDefaultAsync(
                manga => manga.UserId == userId && manga.MalId == malId,
                cancellationToken);
    }

    public Task<MangaCollectionItem?> GetByUserIdAndIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        {
            return _dbContext.MangaCollectionItems
                .Include(manga => manga.OwnedVolumes)
                .FirstOrDefaultAsync(
                    manga => manga.UserId == userId && manga.Id == id,
                    cancellationToken);
        }

    public async Task AddAsync(
        MangaCollectionItem manga,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.MangaCollectionItems.AddAsync(manga, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
    public Task RemoveAsync(
    MangaCollectionItem mangaCollectionItem,
    CancellationToken cancellationToken = default)
    {
        _dbContext.MangaCollectionItems.Remove(mangaCollectionItem);

        return Task.CompletedTask;
    }
}