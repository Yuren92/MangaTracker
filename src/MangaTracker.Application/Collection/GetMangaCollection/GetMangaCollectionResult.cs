using MangaTracker.Application.Collection.Dtos;

namespace MangaTracker.Application.Collection.GetMangaCollection;

public sealed record GetMangaCollectionResult(
    IReadOnlyCollection<MangaCollectionItemDto> Items
);