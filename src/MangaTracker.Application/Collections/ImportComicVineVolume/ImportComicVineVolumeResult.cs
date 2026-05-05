namespace MangaTracker.Application.Collections.ImportComicVineVolume;

public sealed record ImportComicVineVolumeResult(
    Guid EditionId,
    Guid UserCollectionId,
    int ComicVineVolumeId,
    string Title,
    string? PublisherName,
    int TotalIssues,
    int ImportedTomes,
    bool IsCompleted);