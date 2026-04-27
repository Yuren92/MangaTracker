namespace MangaTracker.Application.Collection.Dtos;

public sealed record MangaCollectionDetailDto(
    Guid Id,
    int MalId,
    string Title,
    string? ImageUrl,
    int? EffectiveTotalVolumes,
    int OwnedVolumesCount,
    IReadOnlyCollection<OwnedVolumeDto> OwnedVolumes,
    IReadOnlyCollection<int> MissingVolumeNumbers
);  