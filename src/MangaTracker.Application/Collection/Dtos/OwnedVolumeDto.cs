namespace MangaTracker.Application.Collection.Dtos;

public sealed record OwnedVolumeDto(
    int VolumeNumber,
    DateOnly? PurchaseDate,
    decimal? Price,
    string? Store
);