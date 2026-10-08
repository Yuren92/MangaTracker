using MangaTracker.Api.RateLimiting;
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

    public CatalogController(SearchCatalogHandler searchCatalogHandler)
    {
        _searchCatalogHandler = searchCatalogHandler;
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
}
