using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.Entities;

public sealed class Edition
{
    private readonly List<Tome> _tomes = [];
    private readonly List<UserCollection> _userCollections = [];

    public Guid Id { get; private set; }
    public Guid SeriesId { get; private set; }
    public Series Series { get; private set; } = null!;

    public int ComicVineVolumeId { get; private set; }
    public string ComicVineApiDetailUrl { get; private set; }
    public string Name { get; private set; }
    public string? PublisherName { get; private set; }
    public int? StartYear { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? SiteDetailUrl { get; private set; }
    public int? IssueCount { get; private set; }
    public DateTimeOffset ImportedAt { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }

    public IReadOnlyCollection<Tome> Tomes => _tomes.AsReadOnly();
    public IReadOnlyCollection<UserCollection> UserCollections => _userCollections.AsReadOnly();

    private Edition()
    {
        ComicVineApiDetailUrl = string.Empty;
        Name = string.Empty;
    }

    public Edition(
        Guid seriesId,
        int comicVineVolumeId,
        string comicVineApiDetailUrl,
        string name,
        string? publisherName = null,
        int? startYear = null,
        string? description = null,
        string? imageUrl = null,
        string? siteDetailUrl = null,
        int? issueCount = null)
    {
        if (seriesId == Guid.Empty)
        {
            throw new DomainException("Series id is required.");
        }

        if (comicVineVolumeId <= 0)
        {
            throw new DomainException("Comic Vine volume id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(comicVineApiDetailUrl))
        {
            throw new DomainException("Comic Vine volume API detail URL is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Edition name is required.");
        }

        Id = Guid.NewGuid();
        SeriesId = seriesId;
        ComicVineVolumeId = comicVineVolumeId;
        ComicVineApiDetailUrl = comicVineApiDetailUrl.Trim();
        Name = name.Trim();
        PublisherName = NormalizeOptionalText(publisherName);
        StartYear = NormalizeYear(startYear);
        Description = NormalizeOptionalText(description);
        ImageUrl = NormalizeOptionalText(imageUrl);
        SiteDetailUrl = NormalizeOptionalText(siteDetailUrl);
        IssueCount = NormalizeIssueCount(issueCount);
        ImportedAt = DateTimeOffset.UtcNow;
    }

    public void SyncDetails(
        string name,
        string? publisherName,
        int? startYear,
        string? description,
        string? imageUrl,
        string? siteDetailUrl,
        int? issueCount)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Edition name is required.");
        }

        Name = name.Trim();
        PublisherName = NormalizeOptionalText(publisherName);
        StartYear = NormalizeYear(startYear);
        Description = NormalizeOptionalText(description);
        ImageUrl = NormalizeOptionalText(imageUrl);
        SiteDetailUrl = NormalizeOptionalText(siteDetailUrl);
        IssueCount = NormalizeIssueCount(issueCount);
        LastSyncedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static int? NormalizeYear(int? year)
    {
        return year is null or <= 0
            ? null
            : year;
    }

    private static int? NormalizeIssueCount(int? issueCount)
    {
        return issueCount is null or <= 0
            ? null
            : issueCount;
    }
}