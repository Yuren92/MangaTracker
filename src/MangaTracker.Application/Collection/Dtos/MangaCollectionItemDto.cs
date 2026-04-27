namespace MangaTracker.Application.Collection.Dtos;

public sealed record MangaCollectionItemDto(
    Guid Id,
    int MalId,
    string Title,
    string? ImageUrl,
    int? EffectiveTotalVolumes,
    int OwnedVolumesCount
);