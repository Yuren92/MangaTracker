namespace MangaTracker.Application.Mal.Dtos;

public sealed record MalMangaRecommendationDto(
	int MalId,
	string Title,
	string? ImageUrl,
	int NumRecommendations
);