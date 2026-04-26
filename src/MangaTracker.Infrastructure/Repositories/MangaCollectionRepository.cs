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

    public Task<MangaCollectionItem?> GetByMalIdAsync(
        int malId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MangaCollectionItems
            .FirstOrDefaultAsync(manga => manga.MalId == malId, cancellationToken);
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
}