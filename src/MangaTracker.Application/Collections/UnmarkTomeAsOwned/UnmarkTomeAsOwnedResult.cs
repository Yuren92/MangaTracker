namespace MangaTracker.Application.Collections.UnmarkTomeAsOwned;

public sealed record UnmarkTomeAsOwnedResult(
    Guid CollectionId,
    Guid TomeId,
    bool IsOwned,
    int TotalTomes,
    int OwnedTomes,
    int PendingTomes);