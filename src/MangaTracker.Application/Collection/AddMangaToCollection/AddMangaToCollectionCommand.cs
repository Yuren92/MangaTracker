namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed record AddMangaToCollectionCommand(
    int MalId,
    string Title,
    string? ImageUrl,
    int? MalTotalVolumes,
    int? CustomTotalVolumes
);