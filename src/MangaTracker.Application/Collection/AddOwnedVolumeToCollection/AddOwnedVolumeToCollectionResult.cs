namespace MangaTracker.Application.Collection.AddOwnedVolumeToCollection;

public sealed record AddOwnedVolumeToCollectionResult(
    Guid CollectionItemId,
    int VolumeNumber,
    int OwnedVolumesCount
);