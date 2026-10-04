using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.Entities;

public sealed class Tome
{
    private readonly List<UserOwnedTome> _userOwnedTomes = [];

    public Guid Id { get; private set; }
    public Guid EditionId { get; private set; }
    public Edition Edition { get; private set; } = null!;

    public int ComicVineIssueId { get; private set; }
    public string ComicVineApiDetailUrl { get; private set; }
    public string IssueNumber { get; private set; }
    public int? NormalizedNumber { get; private set; }
    public string? Title { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateOnly? CoverDate { get; private set; }
    public DateOnly? StoreDate { get; private set; }
    public string? SiteDetailUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<UserOwnedTome> UserOwnedTomes => _userOwnedTomes.AsReadOnly();

    private Tome()
    {
        ComicVineApiDetailUrl = string.Empty;
        IssueNumber = string.Empty;
    }

    public Tome(
        Guid editionId,
        int comicVineIssueId,
        string comicVineApiDetailUrl,
        string issueNumber,
        int? normalizedNumber,
        string? title = null,
        string? imageUrl = null,
        DateOnly? coverDate = null,
        DateOnly? storeDate = null,
        string? siteDetailUrl = null)
    {
        if (editionId == Guid.Empty)
        {
            throw new DomainException("Edition id is required.");
        }

        if (comicVineIssueId <= 0)
        {
            throw new DomainException("Comic Vine issue id must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(comicVineApiDetailUrl))
        {
            throw new DomainException("Comic Vine issue API detail URL is required.");
        }

        Id = Guid.NewGuid();
        EditionId = editionId;
        ComicVineIssueId = comicVineIssueId;
        ComicVineApiDetailUrl = comicVineApiDetailUrl.Trim();
        IssueNumber = NormalizeIssueNumber(issueNumber);
        NormalizedNumber = NormalizeNumber(normalizedNumber);
        Title = NormalizeOptionalText(title);
        ImageUrl = NormalizeOptionalText(imageUrl);
        CoverDate = coverDate;
        StoreDate = storeDate;
        SiteDetailUrl = NormalizeOptionalText(siteDetailUrl);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void SyncDetails(
        string issueNumber,
        int? normalizedNumber,
        string? title,
        string? imageUrl,
        DateOnly? coverDate,
        DateOnly? storeDate,
        string? siteDetailUrl)
    {
        IssueNumber = NormalizeIssueNumber(issueNumber);
        NormalizedNumber = NormalizeNumber(normalizedNumber);
        Title = NormalizeOptionalText(title);
        ImageUrl = NormalizeOptionalText(imageUrl);
        CoverDate = coverDate;
        StoreDate = storeDate;
        SiteDetailUrl = NormalizeOptionalText(siteDetailUrl);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Specials and one-shots can come from Comic Vine without a number. They are still
    // tomes (identified by their Comic Vine id); they just sort last.
    private static string NormalizeIssueNumber(string? issueNumber)
    {
        return issueNumber?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static int? NormalizeNumber(int? number)
    {
        return number is null or <= 0
            ? null
            : number;
    }
}