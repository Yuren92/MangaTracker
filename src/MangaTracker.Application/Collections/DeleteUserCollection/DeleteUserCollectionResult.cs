namespace MangaTracker.Application.Collections.DeleteUserCollection;

public sealed record DeleteUserCollectionResult(
    Guid CollectionId,
    bool Deleted);