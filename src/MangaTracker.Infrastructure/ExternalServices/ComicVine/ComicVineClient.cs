using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace MangaTracker.Infrastructure.ExternalServices.ComicVine;

public sealed class ComicVineClient : IComicVineClient
{
    // Comic Vine's maximum page size for list resources.
    private const int PageSize = 100;

    // Safety cap for paging; imports reject volumes with more than 250 issues anyway.
    private const int MaxIssuesPerVolume = 1000;

    // field_list keeps responses to what is mapped below; a full issue or volume also
    // carries characters, teams, credits... that the app never reads.
    private const string SearchFields =
        "id,name,publisher,count_of_issues,image,start_year,deck,site_detail_url,api_detail_url";

    private const string VolumeFields =
        "id,name,publisher,count_of_issues,image,start_year,description,site_detail_url,api_detail_url,issues";

    private const string IssueFields =
        "id,issue_number,name,image,cover_date,store_date,site_detail_url,api_detail_url";

    // Short-lived cache for what users ask for repeatedly: the same search typed by many
    // people, and a volume previewed and then added a moment later (which used to fetch
    // it twice). Long enough to save those requests, short enough that new issues show
    // up within minutes; the daily catalog sync compares issue counts, not cached data.
    private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan VolumeCacheDuration = TimeSpan.FromMinutes(10);

    private readonly HttpClient _httpClient;
    private readonly ComicVineOptions _options;
    private readonly IMemoryCache _cache;

    public ComicVineClient(
        HttpClient httpClient,
        IOptions<ComicVineOptions> options,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> SearchVolumesAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return [];
        }

        var safeLimit = Math.Clamp(limit, 1, 50);
        var cacheKey = $"comicvine:search:{safeLimit}:{cleanQuery.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyCollection<ComicVineVolumeSearchResultDto>? cached))
        {
            return cached!;
        }

        var results = await FetchSearchAsync(cleanQuery, safeLimit, cancellationToken);
        _cache.Set(cacheKey, results, SearchCacheDuration);

        return results;
    }

    public async Task<IReadOnlyDictionary<int, int>> GetVolumeIssueCountsAsync(
        IReadOnlyCollection<int> comicVineVolumeIds,
        CancellationToken cancellationToken = default)
    {
        var counts = new Dictionary<int, int>();

        // Comic Vine filters accept several values separated by "|", so one request
        // covers up to a full page of volumes.
        foreach (var batch in comicVineVolumeIds.Where(id => id > 0).Distinct().Chunk(PageSize))
        {
            var url = "volumes/" +
                      $"?api_key={Uri.EscapeDataString(_options.ApiKey)}" +
                      "&format=json" +
                      $"&filter=id:{string.Join('|', batch)}" +
                      "&field_list=id,count_of_issues" +
                      $"&limit={PageSize}";

            var response = await GetComicVineResponseAsync<ComicVineListResponse<ComicVineVolumeResource>>(
                url,
                cancellationToken);

            foreach (var volume in response?.Results ?? [])
            {
                if (volume.Id > 0 && volume.CountOfIssues is int count)
                {
                    counts[volume.Id] = count;
                }
            }
        }

        return counts;
    }

    private async Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> FetchSearchAsync(
        string cleanQuery,
        int safeLimit,
        CancellationToken cancellationToken)
    {

        var url = "search/" +
                  $"?api_key={Uri.EscapeDataString(_options.ApiKey)}" +
                  "&format=json" +
                  "&resources=volume" +
                  $"&query={Uri.EscapeDataString(cleanQuery)}" +
                  $"&field_list={SearchFields}" +
                  $"&limit={safeLimit}";

        var response = await GetComicVineResponseAsync<ComicVineListResponse<ComicVineVolumeResource>>(
            url,
            cancellationToken);

        if (response is null)
        {
            return [];
        }

        return response.Results
            .Select(volume => new ComicVineVolumeSearchResultDto(
                ComicVineVolumeId: volume.Id,
                Name: volume.Name ?? string.Empty,
                PublisherName: volume.Publisher?.Name,
                CountOfIssues: volume.CountOfIssues,
                ImageUrl: GetBestImageUrl(volume.Image),
                StartYear: ParseYear(volume.StartYear),
                Deck: volume.Deck,
                SiteDetailUrl: volume.SiteDetailUrl,
                ApiDetailUrl: volume.ApiDetailUrl ?? string.Empty))
            .Where(volume => !string.IsNullOrWhiteSpace(volume.ApiDetailUrl))
            .ToList();
    }

    public async Task<ComicVineVolumeDetailDto?> GetVolumeByApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var url = BuildComicVineApiUrl(apiDetailUrl, VolumeFields);

        // Keyed by the validated path, never by the raw input or the URL with the key.
        var cacheKey = $"comicvine:volume:{new Uri(url).AbsolutePath.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out ComicVineVolumeDetailDto? cached))
        {
            return cached;
        }

        var volume = await FetchVolumeAsync(url, apiDetailUrl, cancellationToken);

        // "Not found" is not cached: it may be a transient gap on Comic Vine's side.
        if (volume is not null)
        {
            _cache.Set(cacheKey, volume, VolumeCacheDuration);
        }

        return volume;
    }

    private async Task<ComicVineVolumeDetailDto?> FetchVolumeAsync(
        string url,
        string apiDetailUrl,
        CancellationToken cancellationToken)
    {
        var response = await GetComicVineResponseAsync<ComicVineSingleResponse<ComicVineVolumeResource>>(
            url,
            cancellationToken);

        var volume = response?.Results;

        if (volume is null || volume.Id == 0)
        {
            return null;
        }

        var issues = volume.Issues
            .Select(issue => new ComicVineIssueSummaryDto(
                ComicVineIssueId: issue.Id,
                IssueNumber: issue.IssueNumber ?? string.Empty,
                NormalizedNumber: NormalizeIssueNumber(issue.IssueNumber),
                Title: issue.Name,
                SiteDetailUrl: issue.SiteDetailUrl,
                ApiDetailUrl: issue.ApiDetailUrl ?? string.Empty))
            .Where(issue => !string.IsNullOrWhiteSpace(issue.ApiDetailUrl))
            .OrderBy(issue => NormalizeIssueNumber(issue.IssueNumber) ?? int.MaxValue)
            .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ComicVineVolumeDetailDto(
            ComicVineVolumeId: volume.Id,
            Name: volume.Name ?? string.Empty,
            PublisherName: volume.Publisher?.Name,
            CountOfIssues: volume.CountOfIssues,
            ImageUrl: GetBestImageUrl(volume.Image),
            StartYear: ParseYear(volume.StartYear),
            Description: volume.Description,
            SiteDetailUrl: volume.SiteDetailUrl,
            ApiDetailUrl: volume.ApiDetailUrl ?? apiDetailUrl,
            Issues: issues);
    }

    public async Task<IReadOnlyCollection<ComicVineIssueDetailDto>> GetVolumeIssuesAsync(
        int comicVineVolumeId,
        CancellationToken cancellationToken = default)
    {
        if (comicVineVolumeId <= 0)
        {
            return [];
        }

        var issues = new List<ComicVineIssueDetailDto>();

        // The issues list returns at most 100 results per page; the volume's issues are
        // read page by page until Comic Vine reports no more (or the safety cap is hit).
        for (var offset = 0; offset < MaxIssuesPerVolume; offset += PageSize)
        {
            var url = "issues/" +
                      $"?api_key={Uri.EscapeDataString(_options.ApiKey)}" +
                      "&format=json" +
                      $"&filter=volume:{comicVineVolumeId}" +
                      $"&field_list={IssueFields}" +
                      $"&limit={PageSize}" +
                      $"&offset={offset}";

            var page = await GetComicVineResponseAsync<ComicVineListResponse<ComicVineIssueResource>>(
                url,
                cancellationToken);

            if (page is null || page.Results.Count == 0)
            {
                break;
            }

            issues.AddRange(page.Results
                .Where(issue => issue.Id > 0 && !string.IsNullOrWhiteSpace(issue.ApiDetailUrl))
                .Select(issue => new ComicVineIssueDetailDto(
                    ComicVineIssueId: issue.Id,
                    IssueNumber: issue.IssueNumber ?? string.Empty,
                    NormalizedNumber: NormalizeIssueNumber(issue.IssueNumber),
                    Title: issue.Name,
                    ImageUrl: GetBestImageUrl(issue.Image),
                    CoverDate: ParseDateOnly(issue.CoverDate),
                    StoreDate: ParseDateOnly(issue.StoreDate),
                    SiteDetailUrl: issue.SiteDetailUrl,
                    ApiDetailUrl: issue.ApiDetailUrl!)));

            if (offset + page.Results.Count >= page.NumberOfTotalResults)
            {
                break;
            }
        }

        return issues;
    }

    private string BuildComicVineApiUrl(string apiDetailUrl, string fieldList)
    {
        if (string.IsNullOrWhiteSpace(apiDetailUrl))
        {
            throw new ValidationException("Comic Vine API detail URL is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Comic Vine API key is not configured.");
        }

        if (!Uri.TryCreate(apiDetailUrl, UriKind.Absolute, out var uri))
        {
            throw new ValidationException("Comic Vine API detail URL is not valid.");
        }

        // The URL comes from the client, and the API key is appended to it, so it must be
        // pinned to the real Comic Vine API: HTTPS only (the key must never travel in clear
        // text), exact host, default port, no credentials and the /api/ path.
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ValidationException("Comic Vine API detail URL must use HTTPS.");
        }

        if (!string.Equals(uri.Host, "comicvine.gamespot.com", StringComparison.OrdinalIgnoreCase)
            || !uri.IsDefaultPort
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ValidationException("Comic Vine API detail URL must belong to Comic Vine.");
        }

        if (!uri.AbsolutePath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Comic Vine API detail URL must point to the Comic Vine API.");
        }

        // Rebuild from the parsed URI instead of reusing the raw string, so the request
        // that is sent is exactly the one that was validated: normalized path, and no
        // fragment or query from the client (Comic Vine detail URLs never carry one).
        var baseUrl = uri.GetLeftPart(UriPartial.Path);

        return $"{baseUrl}?api_key={Uri.EscapeDataString(_options.ApiKey)}&format=json&field_list={fieldList}";
    }

    // Returns null when Comic Vine answers 404. Transient failures are retried by the
    // resilience pipeline configured for this HttpClient; whatever still fails afterwards
    // (timeouts, 5xx, 429 or Comic Vine's own 420 rate-limit status, open circuit,
    // invalid responses) becomes
    // ExternalServiceUnavailableException so callers and the API can treat it as a 503.
    // https://comicvine.gamespot.com/api/documentation: status_code 1 = OK, 101 = Object Not Found.
    private const int StatusOk = 1;
    private const int StatusObjectNotFound = 101;

    private async Task<T?> GetComicVineResponseAsync<T>(
        string url,
        CancellationToken cancellationToken)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Comic Vine API key is not configured.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            // Comic Vine answers HTTP 200 even for errors and reports the outcome in
            // status_code. "Object Not Found" comes with results as an empty array, which
            // must mean "does not exist", not "provider down".
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (document.RootElement.TryGetProperty("status_code", out var statusCode)
                && statusCode.ValueKind == JsonValueKind.Number)
            {
                switch (statusCode.GetInt32())
                {
                    case StatusOk:
                        break;
                    case StatusObjectNotFound:
                        return null;
                    default:
                        // Invalid API key, malformed URL, rate limit...: nothing the caller can fix.
                        var error = document.RootElement.TryGetProperty("error", out var errorText)
                            ? errorText.GetString()
                            : null;

                        throw new JsonException(
                            $"Comic Vine returned status_code {statusCode.GetInt32()}: {error}");
                }
            }

            return document.RootElement.Deserialize<T>()
                ?? throw new JsonException("Comic Vine returned an empty response.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller cancelled (request aborted): not a provider failure.
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException
                                              or OperationCanceledException
                                              or TimeoutRejectedException
                                              or BrokenCircuitException
                                              or JsonException)
        {
            throw new ExternalServiceUnavailableException(
                "Comic Vine is not available right now. Please try again later.",
                exception);
        }
    }

    // Covers are shown as cards and thumbnails, so a medium rendition is enough. The
    // original scan can weigh several megabytes, which on a page with a hundred pending
    // tomes means hundreds of megabytes; it is only a last resort.
    private static string? GetBestImageUrl(ComicVineImageResource? image)
    {
        return image?.MediumUrl
            ?? image?.ScreenLargeUrl
            ?? image?.ScreenUrl
            ?? image?.SuperUrl
            ?? image?.OriginalUrl
            ?? image?.SmallUrl
            ?? image?.ThumbUrl
            ?? image?.IconUrl
            ?? image?.TinyUrl;
    }

    private static int? ParseYear(string? year)
    {
        return int.TryParse(year, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static DateOnly? ParseDateOnly(string? value)
    {
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static int? NormalizeIssueNumber(string? issueNumber)
    {
        if (string.IsNullOrWhiteSpace(issueNumber))
        {
            return null;
        }

        return int.TryParse(issueNumber.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    private sealed class ComicVineListResponse<TResource>
    {
        [JsonPropertyName("number_of_total_results")]
        public int NumberOfTotalResults { get; init; }

        [JsonPropertyName("results")]
        public List<TResource> Results { get; init; } = [];
    }

    private sealed class ComicVineSingleResponse<TResource>
    {
        [JsonPropertyName("results")]
        public TResource? Results { get; init; }
    }

    private sealed class ComicVineVolumeResource
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("publisher")]
        public ComicVineNamedResource? Publisher { get; init; }

        [JsonPropertyName("count_of_issues")]
        public int? CountOfIssues { get; init; }

        [JsonPropertyName("image")]
        public ComicVineImageResource? Image { get; init; }

        [JsonPropertyName("start_year")]
        public string? StartYear { get; init; }

        [JsonPropertyName("deck")]
        public string? Deck { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("site_detail_url")]
        public string? SiteDetailUrl { get; init; }

        [JsonPropertyName("api_detail_url")]
        public string? ApiDetailUrl { get; init; }

        [JsonPropertyName("issues")]
        public List<ComicVineIssueResource> Issues { get; init; } = [];
    }

    private sealed class ComicVineIssueResource
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("issue_number")]
        public string? IssueNumber { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("image")]
        public ComicVineImageResource? Image { get; init; }

        [JsonPropertyName("cover_date")]
        public string? CoverDate { get; init; }

        [JsonPropertyName("store_date")]
        public string? StoreDate { get; init; }

        [JsonPropertyName("site_detail_url")]
        public string? SiteDetailUrl { get; init; }

        [JsonPropertyName("api_detail_url")]
        public string? ApiDetailUrl { get; init; }
    }

    private sealed class ComicVineNamedResource
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class ComicVineImageResource
    {
        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; init; }

        [JsonPropertyName("medium_url")]
        public string? MediumUrl { get; init; }

        [JsonPropertyName("screen_url")]
        public string? ScreenUrl { get; init; }

        [JsonPropertyName("screen_large_url")]
        public string? ScreenLargeUrl { get; init; }

        [JsonPropertyName("small_url")]
        public string? SmallUrl { get; init; }

        [JsonPropertyName("super_url")]
        public string? SuperUrl { get; init; }

        [JsonPropertyName("thumb_url")]
        public string? ThumbUrl { get; init; }

        [JsonPropertyName("tiny_url")]
        public string? TinyUrl { get; init; }

        [JsonPropertyName("original_url")]
        public string? OriginalUrl { get; init; }
    }
}