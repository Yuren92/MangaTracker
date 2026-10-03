using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface ITomeRepository
{
    // One query for a whole batch of issues, instead of one query per issue.
    Task<IReadOnlyCollection<Tome>> GetByComicVineApiDetailUrlsAsync(
        IReadOnlyCollection<string> apiDetailUrls,
        CancellationToken cancellationToken = default);

    Task<Tome?> GetByIdAsync(
        Guid tomeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Tome tome,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Tome>> GetByEditionIdAsync(
        Guid editionId,
        CancellationToken cancellationToken = default);
}