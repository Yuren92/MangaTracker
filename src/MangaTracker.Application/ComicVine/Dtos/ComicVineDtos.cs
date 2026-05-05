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
    string? Description,
    string? SiteDetailUrl,
    string ApiDetailUrl);