using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class TomeRepository : ITomeRepository
{
    private readonly MangaTrackerDbContext _dbContext;

    public TomeRepository(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Tome?> GetByComicVineApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = apiDetailUrl.Trim();

        return _dbContext.Tomes
            .FirstOrDefaultAsync(
                tome => tome.ComicVineApiDetailUrl == normalizedUrl,
                cancellationToken);
    }

    public Task<Tome?> GetByIdAsync(
        Guid tomeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Tomes
            .FirstOrDefaultAsync(
                tome => tome.Id == tomeId,
                cancellationToken);
    }

    public async Task AddAsync(
        Tome tome,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Tomes.AddAsync(tome, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}