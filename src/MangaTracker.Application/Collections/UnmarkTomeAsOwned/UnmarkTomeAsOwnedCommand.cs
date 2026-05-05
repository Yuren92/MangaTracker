namespace MangaTracker.Application.Collections.UnmarkTomeAsOwned;

public sealed record UnmarkTomeAsOwnedCommand(
    Guid UserId,
    Guid CollectionId,
    Guid TomeId);