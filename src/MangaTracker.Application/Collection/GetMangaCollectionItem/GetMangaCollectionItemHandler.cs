using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Collection.Dtos;
using MangaTracker.Domain.Common;

namespace MangaTracker.Application.Collection.GetMangaCollectionItem;

public sealed class GetMangaCollectionItemHandler
{
    private readonly IMangaCollectionRepository _repository;

    public GetMangaCollectionItemHandler(IMangaCollectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetMangaCollectionItemResult> HandleAsync(
        Guid userId,
        Guid collectionItemId,
        CancellationToken cancellationToken = default)
    {
        var manga = await _repository.GetByUserIdAndIdAsync(
            userId,
            collectionItemId,
            cancellationToken);

        if (manga is null)
        {
            throw new DomainException("Manga collection item was not found.");
        }

        var ownedVolumes = manga.OwnedVolumes
            .OrderBy(volume => volume.Number.Value)
            .Select(volume => new OwnedVolumeDto(
                VolumeNumber: volume.Number.Value,
                PurchaseDate: volume.PurchaseDate,
                Price: volume.Price,
                Store: volume.Store))
            .ToList();

        var missingVolumeNumbers = manga.GetMissingVolumeNumbers()
            .Select(volumeNumber => volumeNumber.Value)
            .ToList();

        var item = new MangaCollectionDetailDto(
            Id: manga.Id,
            MalId: manga.MalId,
            Title: manga.Title,
            ImageUrl: manga.ImageUrl,
            EffectiveTotalVolumes: manga.EffectiveTotalVolumes,
            OwnedVolumesCount: manga.OwnedVolumes.Count,
            OwnedVolumes: ownedVolumes,
            MissingVolumeNumbers: missingVolumeNumbers);

        return new GetMangaCollectionItemResult(item);
    }
}