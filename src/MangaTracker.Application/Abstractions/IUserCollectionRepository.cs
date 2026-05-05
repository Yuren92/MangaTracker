using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface IUserCollectionRepository
{
    Task<UserCollection?> GetByUserIdAndEditionIdAsync(
        Guid userId,
        Guid editionId,
        CancellationToken cancellationToken = default);

    Task<UserCollection?> GetByUserIdAndIdAsync(
        Guid userId,
        Guid collectionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserCollection userCollection,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserCollection>> GetAllByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken = default);
}