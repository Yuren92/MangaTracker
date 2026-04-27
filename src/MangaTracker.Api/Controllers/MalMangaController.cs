using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Mal.GetMalMangaDetail;
using MangaTracker.Application.Mal.SearchMalManga;
using Microsoft.AspNetCore.Mvc;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/mal/manga")]
public sealed class MalMangaController : ControllerBase
{
    private readonly SearchMalMangaHandler _searchMalMangaHandler;
    private readonly GetMalMangaDetailHandler _getMalMangaDetailHandler;
    private readonly ICurrentUserService _currentUserService;

    public MalMangaController(SearchMalMangaHandler searchMalMangaHandler, GetMalMangaDetailHandler getMalMangaDetailHandler, ICurrentUserService currentUserService)
    {
        _searchMalMangaHandler = searchMalMangaHandler;
        _getMalMangaDetailHandler = getMalMangaDetailHandler;
        _currentUserService = currentUserService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<SearchMalMangaResult>> SearchManga(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _searchMalMangaHandler.HandleAsync(
            query,
            limit,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{malId:int}")]
    public async Task<ActionResult<GetMalMangaDetailResult>> GetMangaDetail(
        int malId,
        CancellationToken cancellationToken)
    {
        var result = await _getMalMangaDetailHandler.HandleAsync(
            _currentUserService.UserId,
            malId,
            cancellationToken);

        return Ok(result);
    }
}