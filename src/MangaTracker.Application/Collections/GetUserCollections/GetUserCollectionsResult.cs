namespace MangaTracker.Application.Collections.GetUserCollections;

public sealed record GetUserCollectionsResult(
    IReadOnlyCollection<UserCollectionListItemResult> Items);

public sealed record UserCollectionListItemResult(
    Guid Id,
    Guid EditionId,
    int ComicVineVolumeId,
    string ComicVineApiDetailUrl,
    string Title,
    string? PublisherName,
    string? ImageUrl,
    int TotalTomes,
    int OwnedTomes,
    int PendingTomes,
    // True while the edition's tomes are still being downloaded after it was added.
    bool IsImporting = false);