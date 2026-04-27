namespace MangaTracker.Application.Collection.RemoveOwnedVolumeFromCollection;

public sealed record RemoveOwnedVolumeFromCollectionCommand(
    Guid UserId,
    Guid CollectionItemId,
    int VolumeNumber
);