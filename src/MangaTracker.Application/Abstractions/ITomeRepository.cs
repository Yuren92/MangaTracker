using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface ITomeRepository
{
    Task<Tome?> GetByComicVineApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default);

    Task<Tome?> GetByIdAsync(
        Guid tomeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Tome tome,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Tome>> GetByEditionIdAsync(
        Guid editionId,
        CancellationToken cancellationToken = default);
}