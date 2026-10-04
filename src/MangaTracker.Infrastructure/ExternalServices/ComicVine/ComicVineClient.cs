using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace MangaTracker.Infrastructure.ExternalServices.ComicVine;

public sealed class ComicVineClient : IComicVineClient
{
    private readonly HttpClient _httpClient;
    private readonly ComicVineOptions _options;

    public ComicVineClient(
        HttpClient httpClient,
        IOptions<ComicVineOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
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

        var url = "search/" +
                  $"?api_key={Uri.EscapeDataString(_options.ApiKey)}" +
                  "&format=json" +
                  "&resources=volume" +
                  $"&query={Uri.EscapeDataString(cleanQuery)}" +
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

    public Task<ComicVineVolumeDetailDto?> GetVolumeByComicVineVolumeIdAsync(
        int comicVineVolumeId,
        CancellationToken cancellationToken = default)
    {
        if (comicVineVolumeId <= 0)
        {
            return Task.FromResult<ComicVineVolumeDetailDto?>(null);
        }

        var apiDetailUrl = $"https://comicvine.gamespot.com/api/volume/4050-{comicVineVolumeId}/";

        return GetVolumeByApiDetailUrlAsync(
            apiDetailUrl,
            cancellationToken);
    }

    public async Task<ComicVineVolumeDetailDto?> GetVolumeByApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var url = BuildComicVineApiUrl(apiDetailUrl);

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

    public async Task<ComicVineIssueDetailDto?> GetIssueByApiDetailUrlAsync(
        string apiDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var url = BuildComicVineApiUrl(apiDetailUrl);

        var response = await GetComicVineResponseAsync<ComicVineSingleResponse<ComicVineIssueResource>>(
            url,
            cancellationToken);

        var issue = response?.Results;

        if (issue is null || issue.Id == 0)
        {
            return null;
        }

        return new ComicVineIssueDetailDto(
            ComicVineIssueId: issue.Id,
            IssueNumber: issue.IssueNumber ?? string.Empty,
            NormalizedNumber: NormalizeIssueNumber(issue.IssueNumber),
            Title: issue.Name,
            ImageUrl: GetBestImageUrl(issue.Image),
            CoverDate: ParseDateOnly(issue.CoverDate),
            StoreDate: ParseDateOnly(issue.StoreDate),
            Description: issue.Description,
            SiteDetailUrl: issue.SiteDetailUrl,
            ApiDetailUrl: issue.ApiDetailUrl ?? apiDetailUrl);
    }

    private string BuildComicVineApiUrl(string apiDetailUrl)
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
        // that is sent is exactly the one that was validated (no fragment, normalized path).
        var baseUrl = uri.GetLeftPart(UriPartial.Path);

        var separator = string.IsNullOrEmpty(uri.Query)
            ? "?"
            : "&";

        return $"{baseUrl}{uri.Query}{separator}api_key={Uri.EscapeDataString(_options.ApiKey)}&format=json";
    }

    // Returns null when Comic Vine answers 404. Transient failures are retried by the
    // resilience pipeline configured for this HttpClient; whatever still fails afterwards
    // (timeouts, 5xx, 429, open circuit, invalid responses) becomes
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

    private static string? GetBestImageUrl(ComicVineImageResource? image)
    {
        return image?.OriginalUrl
            ?? image?.SuperUrl
            ?? image?.ScreenLargeUrl
            ?? image?.ScreenUrl
            ?? image?.MediumUrl
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

        [JsonPropertyName("description")]
        public string? Description { get; init; }

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