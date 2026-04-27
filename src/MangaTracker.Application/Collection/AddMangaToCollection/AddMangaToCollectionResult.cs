namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed record AddMangaToCollectionResult(
    Guid UserId,
    Guid Id,
    int MalId,
    string Title,
    string? ImageUrl,
    int? EffectiveTotalVolumes
);