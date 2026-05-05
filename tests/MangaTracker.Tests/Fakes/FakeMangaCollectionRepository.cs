using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Tests.Fakes;

public sealed class FakeMangaCollectionRepository : IMangaCollectionRepository
{
    private readonly List<MangaCollectionItem> _items = [];

    public Task<IReadOnlyCollection<MangaCollectionItem>> GetAllByUserIdAsync(
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        var items = _items
            .Where(manga => manga.UserId == userId)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<MangaCollectionItem>>(items);
    }

    public Task<MangaCollectionItem?> GetByUserIdAndMalIdAsync(
    Guid userId,
    int malId,
    CancellationToken cancellationToken = default)
    {
        var item = _items.FirstOrDefault(manga =>
            manga.UserId == userId &&
            manga.MalId == malId);

        return Task.FromResult(item);
    }

    public Task<MangaCollectionItem?> GetByUserIdAndIdAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default)
        {
            var item = _items.FirstOrDefault(manga =>
                manga.UserId == userId &&
                manga.Id == id);

            return Task.FromResult(item);
        }

    public Task AddAsync(
        MangaCollectionItem manga,
        CancellationToken cancellationToken = default)
    {
        _items.Add(manga);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        MangaCollectionItem mangaCollectionItem,
        CancellationToken cancellationToken = default)
    {
        _items.Remove(mangaCollectionItem);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}