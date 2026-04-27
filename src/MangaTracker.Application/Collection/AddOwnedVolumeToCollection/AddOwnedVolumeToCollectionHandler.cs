using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Common;
using MangaTracker.Domain.ValueObjects;

namespace MangaTracker.Application.Collection.AddOwnedVolumeToCollection;

public sealed class AddOwnedVolumeToCollectionHandler
{
    private readonly IMangaCollectionRepository _repository;

    public AddOwnedVolumeToCollectionHandler(IMangaCollectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<AddOwnedVolumeToCollectionResult> HandleAsync(
        AddOwnedVolumeToCollectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var manga = await _repository.GetByUserIdAndIdAsync(
            command.UserId,
            command.CollectionItemId,
            cancellationToken);

        if (manga is null)
        {
            throw new DomainException("Manga collection item was not found.");
        }

        var volumeNumber = new VolumeNumber(command.VolumeNumber);

        manga.AddOwnedVolume(
            volumeNumber,
            command.PurchaseDate,
            command.Price,
            command.Store);

        await _repository.SaveChangesAsync(cancellationToken);

        return new AddOwnedVolumeToCollectionResult(
            CollectionItemId: manga.Id,
            VolumeNumber: volumeNumber.Value,
            OwnedVolumesCount: manga.OwnedVolumes.Count);
    }
}