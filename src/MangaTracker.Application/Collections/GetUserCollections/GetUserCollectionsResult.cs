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
    int PendingTomes);