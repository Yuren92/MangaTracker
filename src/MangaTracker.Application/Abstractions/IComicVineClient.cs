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

    // Every issue of a volume with the details a tome needs, fetched in pages of 100
    // through the issues list instead of one request per issue.
    Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetVolumeIssuesAsync(
        int comicVineVolumeId,
        CancellationToken cancellationToken = default);
}
