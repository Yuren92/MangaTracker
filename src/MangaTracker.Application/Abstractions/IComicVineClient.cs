using MangaTracker.Application.ComicVine.Dtos;

namespace MangaTracker.Application.Abstractions;

public interface IComicVineClient
{
    Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> SearchVolumesAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<ComicVineVolumeDetailDto?> GetVolumeByApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default);

    // Name, publisher, cover and issue count of up to 100 volumes per request: enough to
    // refresh an edition and to tell whether it has new issues. Volumes Comic Vine no
    // longer has are left out.
    Task<IReadOnlyCollection<ComicVineVolumeSummaryDto>> GetVolumeSummariesAsync(
        IReadOnlyCollection<int> comicVineVolumeIds,
        CancellationToken cancellationToken = default);

    // Every issue of a volume with the details a tome needs, fetched in pages of 100
    // through the issues list instead of one request per issue.
    Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetVolumeIssuesAsync(
        int comicVineVolumeId,
        CancellationToken cancellationToken = default);

    // Issues added to Comic Vine since a date, for several volumes in the same requests
    // (pages of 100). Each result carries its ComicVineVolumeId.
    Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetIssuesAddedSinceAsync(
        IReadOnlyCollection<int> comicVineVolumeIds,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
