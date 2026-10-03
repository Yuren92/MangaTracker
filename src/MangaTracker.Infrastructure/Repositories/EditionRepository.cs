using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class EditionRepository : IEditionRepository
{
    private readonly MangaTrackerDbContext _dbContext;

    public EditionRepository(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Edition?> GetByComicVineApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var normalizedUrl = apiDetailUrl.Trim();

        return _dbContext.Editions
            .FirstOrDefaultAsync(
                edition => edition.ComicVineApiDetailUrl == normalizedUrl,
                cancellationToken);
    }

    public Task<Edition?> GetByIdAsync(
        Guid editionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Editions
            .FirstOrDefaultAsync(
                edition => edition.Id == editionId,
                cancellationToken);
    }

    public async Task AddAsync(
        Edition edition,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Editions.AddAsync(edition, cancellationToken);
    }
}