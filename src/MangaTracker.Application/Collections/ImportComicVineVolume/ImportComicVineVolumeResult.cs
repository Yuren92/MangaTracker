namespace MangaTracker.Application.Collections.ImportComicVineVolume;

/// <param name="ImportedTomes">Tomes stored when the request finished.</param>
/// <param name="IsCompleted">True when every issue of the volume has a tome stored.</param>
/// <param name="TomesPending">True while the tomes are still being downloaded in the background.</param>
public sealed record ImportComicVineVolumeResult(
    Guid EditionId,
    Guid UserCollectionId,
    int ComicVineVolumeId,
    string Title,
    string? PublisherName,
    int TotalIssues,
    int ImportedTomes,
    bool IsCompleted,
    bool TomesPending);
