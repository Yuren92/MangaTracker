namespace MangaTracker.Application.Mal.Dtos;

public sealed record MalMangaSearchResultDto(
    int MalId,
    string Title,
    string? ImageUrl
);