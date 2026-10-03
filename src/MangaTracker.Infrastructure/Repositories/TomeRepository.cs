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

    public async Task<IReadOnlyCollection<Tome>> GetByComicVineApiDetailUrlsAsync(
        IReadOnlyCollection<string> apiDetailUrls,
        CancellationToken cancellationToken = default)
    {
        if (apiDetailUrls.Count == 0)
        {
            return [];
        }

        var normalizedUrls = apiDetailUrls
            .Select(url => url.Trim())
            .Distinct()
            .ToList();

        return await _dbContext.Tomes
            .Where(tome => normalizedUrls.Contains(tome.ComicVineApiDetailUrl))
            .ToListAsync(cancellationToken);
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

    public async Task<IReadOnlyCollection<Tome>> GetByEditionIdAsync(
        Guid editionId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tomes
            .Where(tome => tome.EditionId == editionId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Tome tome,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Tomes.AddAsync(tome, cancellationToken);
    }
}