using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collection.RemoveMangaFromCollection;

public sealed class RemoveMangaFromCollectionHandler
{
    private readonly IMangaCollectionRepository _mangaCollectionRepository;

    public RemoveMangaFromCollectionHandler(
        IMangaCollectionRepository mangaCollectionRepository)
    {
        _mangaCollectionRepository = mangaCollectionRepository;
    }

    public async Task<RemoveMangaFromCollectionResult> HandleAsync(
        RemoveMangaFromCollectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var mangaCollectionItem = await _mangaCollectionRepository.GetByUserIdAndIdAsync(
            command.UserId,
            command.CollectionItemId,
            cancellationToken);

        if (mangaCollectionItem is null ||
            mangaCollectionItem.UserId != command.UserId)
        {
            throw new NotFoundException("Manga collection item was not found.");
        }

        await _mangaCollectionRepository.RemoveAsync(
            mangaCollectionItem,
            cancellationToken);

        await _mangaCollectionRepository.SaveChangesAsync(cancellationToken);

        return new RemoveMangaFromCollectionResult(
            "Manga removed from collection successfully.");
    }
}