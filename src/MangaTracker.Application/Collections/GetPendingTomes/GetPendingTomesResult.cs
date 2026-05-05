namespace MangaTracker.Application.Collections.GetPendingTomes;

public sealed record GetPendingTomesResult(
    IReadOnlyCollection<PendingTomeResult> Items);

public sealed record PendingTomeResult(
    Guid CollectionId,
    Guid EditionId,
    Guid TomeId,
    string SeriesTitle,
    string EditionName,
    string? PublisherName,
    string IssueNumber,
    int? NormalizedNumber,
    string? TomeTitle,
    string? ImageUrl,
    DateOnly? CoverDate,
    DateOnly? StoreDate);