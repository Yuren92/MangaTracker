namespace MangaTracker.Application.ComicVine.Dtos;

public sealed record ComicVineVolumeSearchResultDto(
    int ComicVineVolumeId,
    string Name,
    string? PublisherName,
    int? CountOfIssues,
    string? ImageUrl,
    int? StartYear,
    string? Deck,
    string? SiteDetailUrl,
    string ApiDetailUrl);

public sealed record ComicVineVolumeDetailDto(
    int ComicVineVolumeId,
    string Name,
    string? PublisherName,
    int? CountOfIssues,
    string? ImageUrl,
    int? StartYear,
    string? Description,
    string? SiteDetailUrl,
    string ApiDetailUrl,
    IReadOnlyCollection<ComicVineIssueSummaryDto> Issues);

// What the batched volumes list returns: enough to refresh an edition and to compare its
// issue count with the tomes stored.
public sealed record ComicVineVolumeSummaryDto(
    int ComicVineVolumeId,
    string Name,
    string? PublisherName,
    int CountOfIssues,
    string? ImageUrl,
    int? StartYear,
    string? SiteDetailUrl);

public sealed record ComicVineIssueSummaryDto(
    int ComicVineIssueId,
    string IssueNumber,
    int? NormalizedNumber,
    string? Title,
    string? SiteDetailUrl,
    string ApiDetailUrl);

public sealed record ComicVineIssueDetailDto(
    int ComicVineIssueId,
    string IssueNumber,
    int? NormalizedNumber,
    string? Title,
    string? ImageUrl,
    DateOnly? CoverDate,
    DateOnly? StoreDate,
    string? SiteDetailUrl,
    string ApiDetailUrl,
    // Set when the issue comes from a list covering several volumes.
    int? ComicVineVolumeId = null);