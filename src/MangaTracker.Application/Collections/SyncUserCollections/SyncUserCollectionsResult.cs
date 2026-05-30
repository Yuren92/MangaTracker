namespace MangaTracker.Application.Collections.SyncUserCollections;

public sealed record SyncUserCollectionsResult(
    int CheckedCollections,
    int SyncedCollections,
    int SkippedCollections,
    int NewTomes);