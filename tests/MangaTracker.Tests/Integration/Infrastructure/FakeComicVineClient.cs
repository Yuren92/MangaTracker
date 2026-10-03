using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Tests.Fakes;

namespace MangaTracker.Tests.Integration.Infrastructure;

// In-memory Comic Vine catalog: three volumes with three issues each. Reads of the
// "slow" volume are delayed so concurrent imports of it overlap and really race.
public sealed class FakeComicVineClient : IComicVineClient
{
    public const string OnePieceUrl = "https://comicvine.gamespot.com/api/volume/4050-1/";
    public const string BerserkUrl = "https://comicvine.gamespot.com/api/volume/4050-2/";
    public const string UnavailableVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-503/";
    public const string SlowVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-3/";

    private readonly Dictionary<string, ComicVineVolumeDetailDto> _volumes = new(StringComparer.OrdinalIgnoreCase)
    {
        [OnePieceUrl] = Volume(1, "One Piece", OnePieceUrl, firstIssue: 1),
        [BerserkUrl] = Volume(2, "Berserk", BerserkUrl, firstIssue: 11),
        [SlowVolumeUrl] = Volume(3, "Naruto", SlowVolumeUrl, firstIssue: 21)
    };

    public Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> SearchVolumesAsync(
        string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<ComicVineVolumeSearchResultDto>>([]);
    }

    public Task<ComicVineVolumeDetailDto?> GetVolumeByComicVineVolumeIdAsync(
        int comicVineVolumeId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_volumes.Values.FirstOrDefault(volume => volume.ComicVineVolumeId == comicVineVolumeId));
    }

    public async Task<ComicVineVolumeDetailDto?> GetVolumeByApiDetailUrlAsync(
        string apiDetailUrl, CancellationToken cancellationToken = default)
    {
        if (string.Equals(apiDetailUrl, UnavailableVolumeUrl, StringComparison.OrdinalIgnoreCase))
        {
            throw new ExternalServiceUnavailableException("Comic Vine is not available right now. Please try again later.", new HttpRequestException("503"));
        }

        if (string.Equals(apiDetailUrl, SlowVolumeUrl, StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }

        return _volumes.GetValueOrDefault(apiDetailUrl);
    }

    public Task<ComicVineIssueDetailDto?> GetIssueByApiDetailUrlAsync(
        string apiDetailUrl, CancellationToken cancellationToken = default)
    {
        var issue = _volumes.Values
            .SelectMany(volume => volume.Issues)
            .FirstOrDefault(issue => string.Equals(issue.ApiDetailUrl, apiDetailUrl, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(issue is null ? null : ComicVineIssues.Detail(issue.NormalizedNumber!.Value));
    }

    private static ComicVineVolumeDetailDto Volume(int id, string name, string url, int firstIssue)
    {
        var issues = Enumerable.Range(firstIssue, 3).Select(ComicVineIssues.Summary).ToList();

        return new ComicVineVolumeDetailDto(
            ComicVineVolumeId: id,
            Name: name,
            PublisherName: "Test Publisher",
            CountOfIssues: issues.Count,
            ImageUrl: null,
            StartYear: 2000,
            Description: null,
            SiteDetailUrl: null,
            ApiDetailUrl: url,
            Issues: issues);
    }
}
