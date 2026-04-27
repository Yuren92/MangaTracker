namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed record AddMangaToCollectionCommand(
    Guid UserId,
    int MalId,
    int? CustomTotalVolumes
);