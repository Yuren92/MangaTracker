using System.Net.Http.Json;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Mal.Dtos;
using Microsoft.Extensions.Options;

namespace MangaTracker.Infrastructure.ExternalServices.MyAnimeList;

public sealed class MalMangaClient : IMalMangaClient
{
    private readonly HttpClient _httpClient;
    private readonly MalOptions _options;

    public MalMangaClient(
        HttpClient httpClient,
        IOptions<MalOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<MalMangaDetailDto?> GetMangaDetailAsync(
        int malId,
        CancellationToken cancellationToken = default)
    {
        var fields = string.Join(',', new[]
        {
            "id",
            "title",
            "main_picture",
            "synopsis",
            "status",
            "num_volumes",
            "num_chapters",
            "recommendations"

        });

        var url = $"manga/{malId}?fields={fields}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MAL-CLIENT-ID", _options.ClientId);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var malResponse = await response.Content.ReadFromJsonAsync<MalMangaResponse>(
            cancellationToken);

        if (malResponse is null)
        {
            return null;
        }

        var recommendations = malResponse.Recommendations
            .Select(recommendation => new MalMangaRecommendationDto(
                MalId: recommendation.Node.Id,
                Title: recommendation.Node.Title,
                ImageUrl: recommendation.Node.MainPicture?.Large ?? recommendation.Node.MainPicture?.Medium,
                NumRecommendations: recommendation.NumRecommendations))
            .ToList();

        return new MalMangaDetailDto(
            MalId: malResponse.Id,
            Title: malResponse.Title,
            ImageUrl: malResponse.MainPicture?.Large ?? malResponse.MainPicture?.Medium,
            TotalVolumes: NormalizeTotal(malResponse.NumVolumes),
            TotalChapters: NormalizeTotal(malResponse.NumChapters),
            Status: malResponse.Status,
            Synopsis: malResponse.Synopsis,
            Recommendations: recommendations);
    }

    public async Task<IReadOnlyCollection<MalMangaSearchResultDto>> SearchMangaAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return [];
        }

        var url =
            $"manga?q={Uri.EscapeDataString(cleanQuery)}&limit={limit}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-MAL-CLIENT-ID", _options.ClientId);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var malResponse = await response.Content.ReadFromJsonAsync<MalMangaSearchResponse>(
            cancellationToken);

        if (malResponse is null)
        {
            return [];
        }

        return malResponse.Data
            .Select(item => new MalMangaSearchResultDto(
                MalId: item.Node.Id,
                Title: item.Node.Title,
                ImageUrl: item.Node.MainPicture?.Large ?? item.Node.MainPicture?.Medium))
            .ToList();
    }

    private static int? NormalizeTotal(int? total)
    {
        if (total is null || total <= 0)
        {
            return null;
        }

        return total;
    }
}