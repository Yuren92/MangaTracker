using MangaTracker.Application.Catalog.SyncCatalog;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface IEditionRepository
{
    Task<Edition?> GetByComicVineApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default);

    // Loads the edition with its tomes, ready to add the ones Comic Vine has published since.
    Task<Edition?> GetByIdWithTomesAsync(
        Guid editionId,
        CancellationToken cancellationToken = default);

    // Every edition someone collects, with how many tomes are stored for it. A cheap
    // read-only projection: the sync only loads the editions that turn out to need it.
    Task<IReadOnlyCollection<EditionSyncCandidate>> GetSyncCandidatesAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Edition edition,
        CancellationToken cancellationToken = default);
}
