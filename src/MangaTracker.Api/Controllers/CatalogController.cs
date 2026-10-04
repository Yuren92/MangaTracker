using MangaTracker.Api.RateLimiting;
using MangaTracker.Application.Catalog.PreviewComicVineVolume;
using MangaTracker.Application.Catalog.SearchCatalog;
using MangaTracker.Application.ComicVine.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize]
public sealed class CatalogController : ControllerBase
{
    private readonly SearchCatalogHandler _searchCatalogHandler;
    private readonly PreviewComicVineVolumeHandler _previewComicVineVolumeHandler;

    public CatalogController(
        SearchCatalogHandler searchCatalogHandler,
        PreviewComicVineVolumeHandler previewComicVineVolumeHandler)
    {
        _searchCatalogHandler = searchCatalogHandler;
        _previewComicVineVolumeHandler = previewComicVineVolumeHandler;
    }

    [EnableRateLimiting(RateLimitPolicies.ExternalApi)]
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyCollection<ComicVineVolumeSearchResultDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var results = await _searchCatalogHandler.HandleAsync(
            new SearchCatalogQuery(query, limit),
            cancellationToken);

        return Ok(results);
    }

    [EnableRateLimiting(RateLimitPolicies.ExternalApi)]
    [HttpPost("comic-vine/volumes/preview")]
    public async Task<ActionResult<ComicVineVolumeDetailDto>> PreviewComicVineVolume(
        [FromBody] ComicVineVolumePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var volume = await _previewComicVineVolumeHandler.HandleAsync(
            new PreviewComicVineVolumeQuery(request.ApiDetailUrl),
            cancellationToken);

        return Ok(volume);
    }
}

public sealed record ComicVineVolumePreviewRequest(
    string ApiDetailUrl);
