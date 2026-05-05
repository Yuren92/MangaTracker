using MangaTracker.Application.ComicVine.Dtos;

namespace MangaTracker.Application.Abstractions;

public interface IComicVineClient
{
    Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> SearchVolumesAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<ComicVineVolumeDetailDto?> GetVolumeByComicVineVolumeIdAsync(
    int comicVineVolumeId,
    CancellationToken cancellationToken = default);

    Task<ComicVineVolumeDetailDto?> GetVolumeByApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default);

    Task<ComicVineIssueDetailDto?> GetIssueByApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default);
}