namespace MangaTracker.Application.Collections.MarkTomeAsOwned;

public sealed record MarkTomeAsOwnedResult(
    Guid CollectionId,
    Guid TomeId,
    bool IsOwned,
    int TotalTomes,
    int OwnedTomes,
    int PendingTomes);