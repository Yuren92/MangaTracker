namespace MangaTracker.Application.Mal.Dtos;

public sealed record MalMangaDetailDto(
    int MalId,
    string Title,
    string? ImageUrl,
    int? TotalVolumes,
    int? TotalChapters,
    string? Status,
    string? Synopsis,
    IReadOnlyCollection<MalMangaRecommendationDto> Recommendations
);