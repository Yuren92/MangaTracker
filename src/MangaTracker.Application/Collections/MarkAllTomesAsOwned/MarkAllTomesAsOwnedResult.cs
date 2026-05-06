namespace MangaTracker.Application.Collections.MarkAllTomesAsOwned;

public sealed record MarkAllTomesAsOwnedResult(
    Guid CollectionId,
    int TotalTomes,
    int OwnedTomes,
    int PendingTomes);