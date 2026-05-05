namespace MangaTracker.Application.Collections.MarkTomeAsOwned;

public sealed record MarkTomeAsOwnedCommand(
    Guid UserId,
    Guid CollectionId,
    Guid TomeId);