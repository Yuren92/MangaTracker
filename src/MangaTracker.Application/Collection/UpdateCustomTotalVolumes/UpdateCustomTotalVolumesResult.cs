namespace MangaTracker.Application.Collection.UpdateCustomTotalVolumes;

public sealed record UpdateCustomTotalVolumesResult(
    Guid CollectionItemId,
    int? CustomTotalVolumes,
    int? EffectiveTotalVolumes
);