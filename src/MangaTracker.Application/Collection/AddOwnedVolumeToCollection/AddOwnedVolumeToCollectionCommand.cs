namespace MangaTracker.Application.Collection.AddOwnedVolumeToCollection;

public sealed record AddOwnedVolumeToCollectionCommand(
    Guid UserId,
    Guid CollectionItemId,
    int VolumeNumber,
    DateOnly? PurchaseDate,
    decimal? Price,
    string? Store
);