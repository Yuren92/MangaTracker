namespace MangaTracker.Application.Collections.SyncUserCollections;

/// <param name="CheckedCollections">All collections of the user.</param>
/// <param name="SyncedCollections">Refreshed from Comic Vine in this run.</param>
/// <param name="SkippedCollections">Not due yet (inside the sync cooldown).</param>
/// <param name="DeferredCollections">Due, but left for a later run because of the per-run limit.</param>
/// <param name="FailedCollections">Due, but Comic Vine could not provide the volume.</param>
/// <param name="NewTomes">Tomes added across all synced collections.</param>
public sealed record SyncUserCollectionsResult(
    int CheckedCollections,
    int SyncedCollections,
    int SkippedCollections,
    int DeferredCollections,
    int FailedCollections,
    int NewTomes);
