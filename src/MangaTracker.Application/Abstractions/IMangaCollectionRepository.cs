using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface IMangaCollectionRepository
{
    Task<MangaCollectionItem?> GetByMalIdAsync(
        int malId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MangaCollectionItem manga,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}