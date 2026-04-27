using MangaTracker.Application.Mal.Dtos;

namespace MangaTracker.Application.Mal.SearchMalManga;

public sealed record SearchMalMangaResult(
    IReadOnlyCollection<MalMangaSearchResultDto> Items
);