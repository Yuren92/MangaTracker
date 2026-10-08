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
    // Two issues share the number 32 (a variant cover), as happens in Comic Vine.
    public const string VariantVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-4/";
    // Throws an unexpected exception whose message must never reach the client.
    public const string BrokenVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-500/";
    public const string SecretInErrorMessage = "Server=prod-sql;Password=hunter2";
    // One issue has no number (a special or one-shot), which Comic Vine sends as null.
    public const string UnnumberedVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-6/";
    public const string SlowVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-3/";
    // Only used by the catalog sync tests, which publish new issues in it.
    public const string GrowingVolumeUrl = "https://comicvine.gamespot.com/api/volume/4050-7/";

    private readonly Dictionary<string, ComicVineVolumeDetailDto> _volumes = new(StringComparer.OrdinalIgnoreCase)
    {
        [OnePieceUrl] = Volume(1, "One Piece", OnePieceUrl, firstIssue: 1),
        [BerserkUrl] = Volume(2, "Berserk", BerserkUrl, firstIssue: 11),
        [SlowVolumeUrl] = Volume(3, "Naruto", SlowVolumeUrl, firstIssue: 21),
        [VariantVolumeUrl] = VariantVolume(),
        [UnnumberedVolumeUrl] = UnnumberedVolume(),
        [GrowingVolumeUrl] = Volume(7, "Dorohedoro", GrowingVolumeUrl, firstIssue: 51)
    };

    public Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> SearchVolumesAsync(
        string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<ComicVineVolumeSearchResultDto>>([]);
    }

    public async Task<ComicVineVolumeDetailDto?> GetVolumeByApiDetailUrlAsync(
        string apiDetailUrl, CancellationToken cancellationToken = default)
    {
        if (string.Equals(apiDetailUrl, BrokenVolumeUrl, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unexpected failure. {SecretInErrorMessage}");
        }

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

    public Task<IReadOnlyCollection<ComicVineVolumeSummaryDto>> GetVolumeSummariesAsync(
        IReadOnlyCollection<int> comicVineVolumeIds, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ComicVineVolumeSummaryDto> summaries = _volumes.Values
            .Where(volume => comicVineVolumeIds.Contains(volume.ComicVineVolumeId))
            .Select(volume => new ComicVineVolumeSummaryDto(
                volume.ComicVineVolumeId,
                volume.Name,
                volume.PublisherName,
                volume.Issues.Count,
                volume.ImageUrl,
                volume.StartYear,
                volume.SiteDetailUrl))
            .ToList();

        return Task.FromResult(summaries);
    }

    // When each issue was added to the fake catalog; the initial ones count as ancient.
    private readonly Dictionary<string, DateTimeOffset> _addedAt = new(StringComparer.OrdinalIgnoreCase);

    // Publishes one more issue in a volume, as Comic Vine does when a new tome comes out.
    public void AddIssue(string volumeUrl, ComicVineIssueSummaryDto issue)
    {
        var volume = _volumes[volumeUrl];
        _volumes[volumeUrl] = volume with
        {
            CountOfIssues = volume.Issues.Count + 1,
            Issues = [.. volume.Issues, issue]
        };
        _addedAt[issue.ApiDetailUrl] = DateTimeOffset.UtcNow;
    }

    public Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetVolumeIssuesAsync(
        int comicVineVolumeId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IssuesOf([comicVineVolumeId], _ => true));
    }

    public Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetIssuesAddedSinceAsync(
        IReadOnlyCollection<int> comicVineVolumeIds, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IssuesOf(
            comicVineVolumeIds,
            issue => _addedAt.GetValueOrDefault(issue.ApiDetailUrl, DateTimeOffset.MinValue) >= since));
    }

    private IReadOnlyCollection<ComicVineIssueDetailDto> IssuesOf(
        IReadOnlyCollection<int> volumeIds,
        Func<ComicVineIssueSummaryDto, bool> include)
    {
        return _volumes.Values
            .Where(volume => volumeIds.Contains(volume.ComicVineVolumeId))
            .SelectMany(volume => volume.Issues
                .Where(include)
                .Select(issue => new ComicVineIssueDetailDto(
                    issue.ComicVineIssueId,
                    issue.IssueNumber,
                    issue.NormalizedNumber,
                    issue.Title,
                    ImageUrl: null,
                    CoverDate: null,
                    StoreDate: null,
                    issue.SiteDetailUrl,
                    issue.ApiDetailUrl,
                    volume.ComicVineVolumeId)))
            .ToList();
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

    private static ComicVineVolumeDetailDto VariantVolume()
    {
        var variant = ComicVineIssues.Summary(32) with
        {
            ComicVineIssueId = 900032,
            Title = "Volume 32 (variant cover)",
            ApiDetailUrl = "https://comicvine.gamespot.com/api/issue/4000-900032/"
        };

        return Volume(4, "Vagabond", VariantVolumeUrl, firstIssue: 31) with
        {
            CountOfIssues = 4,
            Issues = [.. Enumerable.Range(31, 3).Select(ComicVineIssues.Summary), variant]
        };
    }

    private static ComicVineVolumeDetailDto UnnumberedVolume()
    {
        var special = ComicVineIssues.Summary(43) with
        {
            IssueNumber = string.Empty,
            NormalizedNumber = null,
            Title = "Special"
        };

        return Volume(6, "Monster", UnnumberedVolumeUrl, firstIssue: 41) with
        {
            CountOfIssues = 3,
            Issues = [ComicVineIssues.Summary(41), ComicVineIssues.Summary(42), special]
        };
    }
}
