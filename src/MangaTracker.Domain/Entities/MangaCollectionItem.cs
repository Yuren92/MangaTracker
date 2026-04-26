using MangaTracker.Domain.Common;
using MangaTracker.Domain.ValueObjects;

namespace MangaTracker.Domain.Entities;

public sealed class MangaCollectionItem
{
    private readonly List<OwnedVolume> _ownedVolumes = [];

    public Guid Id { get; private set; }
    public int MalId { get; private set; }
    public string Title { get; private set; }
    public string? ImageUrl { get; private set; }
    public int? MalTotalVolumes { get; private set; }
    public int? CustomTotalVolumes { get; private set; }

    public IReadOnlyCollection<OwnedVolume> OwnedVolumes => _ownedVolumes.AsReadOnly();

    private MangaCollectionItem()
    {
        Title = string.Empty;
    }

    public MangaCollectionItem(
        int malId,
        string title,
        string? imageUrl = null,
        int? malTotalVolumes = null,
        int? customTotalVolumes = null)
    {
        if (malId <= 0)
        {
            throw new DomainException("MAL id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        Id = Guid.NewGuid();
        MalId = malId;
        Title = title.Trim();
        ImageUrl = imageUrl;
        MalTotalVolumes = NormalizeTotalVolumes(malTotalVolumes);
        CustomTotalVolumes = NormalizeTotalVolumes(customTotalVolumes);
    }

    public int? EffectiveTotalVolumes => CustomTotalVolumes ?? MalTotalVolumes;

    public void AddOwnedVolume(
        VolumeNumber volumeNumber,
        DateOnly? purchaseDate = null,
        decimal? price = null,
        string? store = null)
        {
            if (EffectiveTotalVolumes is not null && volumeNumber.Value > EffectiveTotalVolumes.Value)
            {
                throw new DomainException("Volume number cannot be greater than total volumes.");
            }

            var alreadyOwned = _ownedVolumes.Any(volume =>
                volume.Number.Value == volumeNumber.Value);

            if (alreadyOwned)
            {
                throw new DomainException("Volume is already owned.");
            }

            var ownedVolume = new OwnedVolume(
                volumeNumber,
                purchaseDate,
                price,
                store);

            _ownedVolumes.Add(ownedVolume);
        }

    public IReadOnlyCollection<VolumeNumber> GetMissingVolumeNumbers()
    {
        if (_ownedVolumes.Count == 0)
        {
            return [];
        }

        var ownedVolumeNumbers = _ownedVolumes
            .Select(volume => volume.Number.Value)
            .ToHashSet();

        var upperLimit = GetMissingVolumeUpperLimit();

        return Enumerable
            .Range(1, upperLimit)
            .Where(volumeNumber => !ownedVolumeNumbers.Contains(volumeNumber))
            .Select(volumeNumber => new VolumeNumber(volumeNumber))
            .ToList();
    }

    private int GetMissingVolumeUpperLimit()
    {
        if (EffectiveTotalVolumes is not null)
        {
            return EffectiveTotalVolumes.Value;
        }

        return _ownedVolumes.Max(volume => volume.Number.Value);
    }

    public void RemoveOwnedVolume(VolumeNumber volumeNumber)
    {
        var ownedVolume = _ownedVolumes.FirstOrDefault(volume =>
            volume.Number.Value == volumeNumber.Value);

        if (ownedVolume is null)
        {
            throw new DomainException("Volume is not owned.");
        }

        _ownedVolumes.Remove(ownedVolume);
    }

    public void SetCustomTotalVolumes(int? totalVolumes)
    {
        var normalizedTotalVolumes = NormalizeTotalVolumes(totalVolumes);

        if (normalizedTotalVolumes is not null && _ownedVolumes.Count > 0)
        {
            var highestOwnedVolume = _ownedVolumes.Max(volume => volume.Number.Value);

            if (highestOwnedVolume > normalizedTotalVolumes.Value)
            {
                throw new DomainException("Total volumes cannot be lower than the highest owned volume.");
            }
        }

        CustomTotalVolumes = normalizedTotalVolumes;
    }

    private static int? NormalizeTotalVolumes(int? totalVolumes)
    {
        if (totalVolumes is null || totalVolumes <= 0)
        {
            return null;
        }

        return totalVolumes;
    }
}