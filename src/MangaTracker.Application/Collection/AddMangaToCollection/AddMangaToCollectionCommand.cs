namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed record AddMangaToCollectionCommand(
    Guid UserId,
    int MalId,
    string Title,
    string? ImageUrl,
    int? MalTotalVolumes,
    int? CustomTotalVolumes
);