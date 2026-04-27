using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Collection.Dtos;

namespace MangaTracker.Application.Collection.GetMangaCollection;

public sealed class GetMangaCollectionHandler
{
    private readonly IMangaCollectionRepository _repository;

    public GetMangaCollectionHandler(IMangaCollectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetMangaCollectionResult> HandleAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        {
        var mangas = await _repository.GetAllByUserIdAsync(userId, cancellationToken);

        var items = mangas
            .Select(manga => new MangaCollectionItemDto(
                Id: manga.Id,
                MalId: manga.MalId,
                Title: manga.Title,
                ImageUrl: manga.ImageUrl,
                EffectiveTotalVolumes: manga.EffectiveTotalVolumes,
                OwnedVolumesCount: manga.OwnedVolumes.Count))
            .ToList();

        return new GetMangaCollectionResult(items);
    }
}