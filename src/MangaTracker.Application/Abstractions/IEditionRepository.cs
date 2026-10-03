using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface IEditionRepository
{
    Task<Edition?> GetByComicVineApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default);

    Task<Edition?> GetByIdAsync(
        Guid editionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Edition edition,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}