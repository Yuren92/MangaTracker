using MangaTracker.Application.Collection.Dtos;

namespace MangaTracker.Application.Collection.GetMangaCollectionItem;

public sealed record GetMangaCollectionItemResult(
    MangaCollectionDetailDto Item
);