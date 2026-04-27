using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Common;
using MangaTracker.Domain.ValueObjects;

namespace MangaTracker.Application.Collection.RemoveOwnedVolumeFromCollection;

public sealed class RemoveOwnedVolumeFromCollectionHandler
{
    private readonly IMangaCollectionRepository _repository;

    public RemoveOwnedVolumeFromCollectionHandler(IMangaCollectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<RemoveOwnedVolumeFromCollectionResult> HandleAsync(
        RemoveOwnedVolumeFromCollectionCommand command,
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

        manga.RemoveOwnedVolume(volumeNumber);

        await _repository.SaveChangesAsync(cancellationToken);

        return new RemoveOwnedVolumeFromCollectionResult(
            CollectionItemId: manga.Id,
            RemovedVolumeNumber: volumeNumber.Value,
            OwnedVolumesCount: manga.OwnedVolumes.Count);
    }
}