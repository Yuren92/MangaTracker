namespace MangaTracker.Application.Collection.RemoveMangaFromCollection;

public sealed record RemoveMangaFromCollectionCommand(
    Guid UserId,
    Guid CollectionItemId
);