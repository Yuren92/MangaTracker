using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.ComicVine.GetIssueDetailsPage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/comic-vine")]
[Authorize]
public sealed class ComicVineController : ControllerBase
{
    private readonly IComicVineClient _comicVineClient;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetIssueDetailsPageHandler _getIssueDetailsPageHandler;

    public ComicVineController(IComicVineClient comicVineClient,
        ICurrentUserService currentUserService,
        GetIssueDetailsPageHandler getIssueDetailsPageHandler)
    {
        _comicVineClient = comicVineClient;
        _currentUserService = currentUserService;
        _getIssueDetailsPageHandler = getIssueDetailsPageHandler;
    }

    [HttpGet("volumes/search")]
    public async Task<ActionResult<IReadOnlyCollection<ComicVineVolumeSearchResultDto>>> SearchVolumes(
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

    [HttpPost("volumes/detail")]
    public async Task<ActionResult<ComicVineVolumeDetailDto>> GetVolumeDetail(
        [FromBody] ComicVineApiDetailUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiDetailUrl))
        {
            return BadRequest("API detail URL is required.");
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

    [HttpPost("issues/detail")]
    public async Task<ActionResult<ComicVineIssueDetailDto>> GetIssueDetail(
        [FromBody] ComicVineApiDetailUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ApiDetailUrl))
        {
            return BadRequest("API detail URL is required.");
        }

        var issue = await _comicVineClient.GetIssueByApiDetailUrlAsync(
            request.ApiDetailUrl,
            cancellationToken);

        if (issue is null)
        {
            return NotFound();
        }

        return Ok(issue);
    }

    [HttpPost("volumes/issues-preview-page")]
    public async Task<ActionResult<GetIssueDetailsPageResult>> GetIssueDetailsPage(
    [FromBody] GetIssueDetailsPageRequest request,
    CancellationToken cancellationToken = default)
    {
        var result = await _getIssueDetailsPageHandler.HandleAsync(
            new GetIssueDetailsPageQuery(
                ApiDetailUrl: request.ApiDetailUrl,
                Offset: request.Offset,
                Limit: request.Limit),
            cancellationToken);

        return Ok(result);
    }

}

public sealed record ImportComicVineVolumePageRequest(
    string ApiDetailUrl,
    int Offset = 0,
    int Limit = 25);

public sealed record ComicVineApiDetailUrlRequest(
    string ApiDetailUrl);

public sealed record GetIssueDetailsPageRequest(
    string ApiDetailUrl,
    int Offset = 0,
    int Limit = 25);