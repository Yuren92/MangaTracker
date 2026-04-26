namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed record AddMangaToCollectionResult(
    Guid Id,
    int MalId,
    string Title,
    string? ImageUrl,
    int? EffectiveTotalVolumes
);