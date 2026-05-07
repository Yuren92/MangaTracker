using MangaTracker.Api.RateLimiting;
using MangaTracker.Application.Abstractions;
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
    private readonly IComicVineClient _comicVineClient;

    public CatalogController(IComicVineClient comicVineClient)
    {
        _comicVineClient = comicVineClient;
    }

    [EnableRateLimiting(RateLimitPolicies.ExternalApi)]
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyCollection<ComicVineVolumeSearchResultDto>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Search query is required.");
        }

        var results = await _comicVineClient.SearchVolumesAsync(
            query,
            limit,
            cancellationToken);

        return Ok(results);
    }

    [EnableRateLimiting(RateLimitPolicies.ExternalApi)]
    [HttpPost("comic-vine/volumes/preview")]
    public async Task<ActionResult<ComicVineVolumeDetailDto>> PreviewComicVineVolume(
    [FromBody] ComicVineVolumePreviewRequest request,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiDetailUrl))
        {
            return BadRequest("Comic Vine volume API detail URL is required.");
        }

        var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
            request.ApiDetailUrl,
            cancellationToken);

        if (volume is null)
        {
            return NotFound();
        }

        return Ok(volume);
    }
}

public sealed record ComicVineVolumePreviewRequest(
    string ApiDetailUrl);