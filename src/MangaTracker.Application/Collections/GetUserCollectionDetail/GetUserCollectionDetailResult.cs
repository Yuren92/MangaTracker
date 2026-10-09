namespace MangaTracker.Application.Collections.GetUserCollectionDetail;

public sealed record GetUserCollectionDetailResult(
    Guid Id,
    Guid EditionId,
    string Title,
    string? PublisherName,
    string? ImageUrl,
    int TotalTomes,
    int OwnedTomes,
    int PendingTomes,
    IReadOnlyCollection<TomeDetailResult> Tomes,
    // True while the edition's tomes are still being downloaded after it was added.
    bool IsImporting = false);

public sealed record TomeDetailResult(
    Guid TomeId,
    int ComicVineIssueId,
    string IssueNumber,
    int? NormalizedNumber,
    string? Title,
    string? ImageUrl,
    DateOnly? CoverDate,
    DateOnly? StoreDate,
    bool IsOwned);