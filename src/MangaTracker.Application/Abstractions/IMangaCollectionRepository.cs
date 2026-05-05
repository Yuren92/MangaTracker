using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface IMangaCollectionRepository
{
    Task<IReadOnlyCollection<MangaCollectionItem>> GetAllByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MangaCollectionItem?> GetByUserIdAndMalIdAsync(
        Guid userId,
        int malId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MangaCollectionItem manga,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<MangaCollectionItem?> GetByUserIdAndIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
    MangaCollectionItem mangaCollectionItem,
    CancellationToken cancellationToken = default);

}