namespace MangaTracker.Application.Collection.RemoveOwnedVolumeFromCollection;

public sealed record RemoveOwnedVolumeFromCollectionResult(
    Guid CollectionItemId,
    int RemovedVolumeNumber,
    int OwnedVolumesCount
);