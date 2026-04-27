namespace MangaTracker.Application.Collection.UpdateCustomTotalVolumes;

public sealed record UpdateCustomTotalVolumesCommand(
    Guid UserId,
    Guid CollectionItemId,
    int? TotalVolumes
);