using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Application.Collections.GetPendingTomes;
using MangaTracker.Application.Collections.GetUserCollectionDetail;
using MangaTracker.Application.Collections.GetUserCollections;
using MangaTracker.Application.Collections.ImportComicVineVolume;
using MangaTracker.Application.Collections.MarkAllTomesAsOwned;
using MangaTracker.Application.Collections.MarkTomeAsOwned;
using MangaTracker.Application.Collections.UnmarkTomeAsOwned;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize]
public sealed class CollectionsController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly GetUserCollectionsHandler _getUserCollectionsHandler;
    private readonly GetUserCollectionDetailHandler _getUserCollectionDetailHandler;
    private readonly MarkTomeAsOwnedHandler _markTomeAsOwnedHandler;
    private readonly UnmarkTomeAsOwnedHandler _unmarkTomeAsOwnedHandler;
    private readonly GetPendingTomesHandler _getPendingTomesHandler;
    private readonly ImportComicVineVolumeHandler _importComicVineVolumeHandler;
    private readonly MarkAllTomesAsOwnedHandler _markAllTomesAsOwnedHandler;

    public CollectionsController(
        ICurrentUserService currentUserService,
        GetUserCollectionsHandler getUserCollectionsHandler,
        GetUserCollectionDetailHandler getUserCollectionDetailHandler,
        MarkTomeAsOwnedHandler markTomeAsOwnedHandler,
        UnmarkTomeAsOwnedHandler unmarkTomeAsOwnedHandler,
        GetPendingTomesHandler getPendingTomesHandler,
        ImportComicVineVolumeHandler importComicVineVolumeHandler,
        MarkAllTomesAsOwnedHandler markAllTomesAsOwnedHandler)
    {
        _currentUserService = currentUserService;
        _getUserCollectionsHandler = getUserCollectionsHandler;
        _getUserCollectionDetailHandler = getUserCollectionDetailHandler;
        _markTomeAsOwnedHandler = markTomeAsOwnedHandler;
        _unmarkTomeAsOwnedHandler = unmarkTomeAsOwnedHandler;
        _getPendingTomesHandler = getPendingTomesHandler;
        _importComicVineVolumeHandler = importComicVineVolumeHandler;
        _markAllTomesAsOwnedHandler = markAllTomesAsOwnedHandler;
    }

    [HttpGet]
    public async Task<ActionResult<GetUserCollectionsResult>> GetUserCollections(
    CancellationToken cancellationToken)
    {
        var result = await _getUserCollectionsHandler.HandleAsync(
            new GetUserCollectionsQuery(_currentUserService.UserId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{collectionId:guid}")]
    public async Task<ActionResult<GetUserCollectionDetailResult>> GetUserCollectionDetail(
    Guid collectionId,
    CancellationToken cancellationToken)
    {
        var result = await _getUserCollectionDetailHandler.HandleAsync(
            new GetUserCollectionDetailQuery(
                UserId: _currentUserService.UserId,
                CollectionId: collectionId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{collectionId:guid}/tomes/{tomeId:guid}/owned")]
    public async Task<ActionResult<MarkTomeAsOwnedResult>> MarkTomeAsOwned(
    Guid collectionId,
    Guid tomeId,
    CancellationToken cancellationToken)
    {
        var result = await _markTomeAsOwnedHandler.HandleAsync(
            new MarkTomeAsOwnedCommand(
                UserId: _currentUserService.UserId,
                CollectionId: collectionId,
                TomeId: tomeId),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{collectionId:guid}/tomes/{tomeId:guid}/owned")]
    public async Task<ActionResult<UnmarkTomeAsOwnedResult>> UnmarkTomeAsOwned(
        Guid collectionId,
        Guid tomeId,
        CancellationToken cancellationToken)
    {
        var result = await _unmarkTomeAsOwnedHandler.HandleAsync(
            new UnmarkTomeAsOwnedCommand(
                UserId: _currentUserService.UserId,
                CollectionId: collectionId,
                TomeId: tomeId),
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("pending-tomes")]
    public async Task<ActionResult<GetPendingTomesResult>> GetPendingTomes(
    CancellationToken cancellationToken)
    {
        var result = await _getPendingTomesHandler.HandleAsync(
            new GetPendingTomesQuery(_currentUserService.UserId),
            cancellationToken);

        return Ok(result);
    }

    [EnableRateLimiting("comic-vine-import")]
    [HttpPost("import-comic-vine-volume")]
    public async Task<ActionResult<ImportComicVineVolumeResult>> ImportComicVineVolume(
    [FromBody] ImportComicVineVolumeRequest request,
    CancellationToken cancellationToken)
    {
        var result = await _importComicVineVolumeHandler.HandleAsync(
            new ImportComicVineVolumeCommand(
                UserId: _currentUserService.UserId,
                ApiDetailUrl: request.ApiDetailUrl),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{collectionId:guid}/tomes/owned-all")]
    public async Task<ActionResult<MarkAllTomesAsOwnedResult>> MarkAllTomesAsOwned(
        Guid collectionId,
        CancellationToken cancellationToken)
    {
        var result = await _markAllTomesAsOwnedHandler.HandleAsync(
            new MarkAllTomesAsOwnedCommand(
                UserId: _currentUserService.UserId,
                CollectionId: collectionId),
            cancellationToken);

        return Ok(result);
    }
}

public sealed record ImportComicVineVolumeRequest(
    string ApiDetailUrl);