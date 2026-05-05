using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class SeriesRepository : ISeriesRepository
{
    private readonly MangaTrackerDbContext _dbContext;

    public SeriesRepository(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Series?> GetByTitleAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.Trim();

        return _dbContext.Series
            .FirstOrDefaultAsync(
                series => series.Title == normalizedTitle,
                cancellationToken);
    }

    public async Task AddAsync(
        Series series,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Series.AddAsync(series, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}