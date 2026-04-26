using MangaTracker.Domain.ValueObjects;

namespace MangaTracker.Domain.Entities;

public sealed class OwnedVolume
{
    public VolumeNumber Number { get; private set; }
    public DateOnly? PurchaseDate { get; private set; }
    public decimal? Price { get; private set; }
    public string? Store { get; private set; }

    private OwnedVolume()
    {
        Number = null!;
    }

    public OwnedVolume(
        VolumeNumber number,
        DateOnly? purchaseDate = null,
        decimal? price = null,
        string? store = null)
    {
        Number = number;
        PurchaseDate = purchaseDate;
        Price = price;
        Store = store;
    }
}