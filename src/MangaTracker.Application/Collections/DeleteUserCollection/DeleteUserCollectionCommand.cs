namespace MangaTracker.Application.Collections.DeleteUserCollection;

public sealed record DeleteUserCollectionCommand(
    Guid UserId,
    Guid CollectionId);