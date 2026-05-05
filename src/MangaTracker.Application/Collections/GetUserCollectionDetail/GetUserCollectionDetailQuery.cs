namespace MangaTracker.Application.Collections.GetUserCollectionDetail;

public sealed record GetUserCollectionDetailQuery(
    Guid UserId,
    Guid CollectionId);