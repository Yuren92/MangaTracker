using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Tests.Fakes;

public sealed class FakeMangaCollectionRepository : IMangaCollectionRepository
{
    private readonly List<MangaCollectionItem> _items = [];

    public Task<MangaCollectionItem?> GetByMalIdAsync(
        int malId,
        CancellationToken cancellationToken = default)
    {
        var item = _items.FirstOrDefault(manga => manga.MalId == malId);

        return Task.FromResult(item);
    }

    public Task AddAsync(
        MangaCollectionItem manga,
        CancellationToken cancellationToken = default)
    {
        _items.Add(manga);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}