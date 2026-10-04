using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions;

public interface ISeriesRepository
{
    Task<Series?> GetByTitleAsync(
        string title,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Series series,
        CancellationToken cancellationToken = default);
}