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

    // How many issues Comic Vine lists for each volume, read for up to 100 volumes per
    // request. Volumes Comic Vine no longer has are left out of the result.
    Task<IReadOnlyDictionary<int, int>> GetVolumeIssueCountsAsync(
        IReadOnlyCollection<int> comicVineVolumeIds,
        CancellationToken cancellationToken = default);

    // Every issue of a volume with the details a tome needs, fetched in pages of 100
    // through the issues list instead of one request per issue.
    Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetVolumeIssuesAsync(
        int comicVineVolumeId,
        CancellationToken cancellationToken = default);
}
