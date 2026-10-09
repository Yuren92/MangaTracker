using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.SyncCatalog;
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

    public Task<Edition?> GetByIdWithTomesAsync(
        Guid editionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Editions
            .Include(edition => edition.Tomes)
            .FirstOrDefaultAsync(
                edition => edition.Id == editionId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<EditionSyncCandidate>> GetSyncCandidatesAsync(
        CancellationToken cancellationToken = default)
    {
        // One query; the tome count is a subquery. Editions nobody collects any more are
        // not worth a Comic Vine request.
        return await _dbContext.Editions
            .AsNoTracking()
            .Where(edition => edition.UserCollections.Any())
            .OrderBy(edition => edition.ComicVineVolumeId)
            .Select(edition => new EditionSyncCandidate(
                edition.Id,
                edition.ComicVineVolumeId,
                edition.Tomes.Count,
                edition.LastSyncedAt ?? edition.ImportedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Edition edition,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Editions.AddAsync(edition, cancellationToken);
    }
}