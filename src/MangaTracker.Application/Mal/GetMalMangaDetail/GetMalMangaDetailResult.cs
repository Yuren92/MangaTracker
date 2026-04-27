using MangaTracker.Application.Mal.Dtos;

namespace MangaTracker.Application.Mal.GetMalMangaDetail;

public sealed record GetMalMangaDetailResult(
    MalMangaDetailWithCollectionStatusDto Item
);