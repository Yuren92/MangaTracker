using MangaTracker.Application.Collection.AddMangaToCollection;
using MangaTracker.Application.Collection.AddOwnedVolumeToCollection;
using MangaTracker.Application.Collection.GetMangaCollection;
using MangaTracker.Application.Collection.GetMangaCollectionItem;
using MangaTracker.Application.Collection.RemoveOwnedVolumeFromCollection;
using MangaTracker.Application.Collection.UpdateCustomTotalVolumes;
using MangaTracker.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/collection")]
public sealed class CollectionController : ControllerBase
{
    private readonly AddMangaToCollectionHandler _addMangaToCollectionHandler;
    private readonly GetMangaCollectionHandler _getMangaCollectionHandler;
    private readonly AddOwnedVolumeToCollectionHandler _addOwnedVolumeToCollectionHandler;
    private readonly RemoveOwnedVolumeFromCollectionHandler _removeOwnedVolumeFromCollectionHandler;
    private readonly GetMangaCollectionItemHandler _getMangaCollectionItemHandler;
    private readonly UpdateCustomTotalVolumesHandler _updateCustomTotalVolumesHandler;
    private readonly ICurrentUserService _currentUserService;


    public CollectionController(AddMangaToCollectionHandler addMangaToCollectionHandler,
        GetMangaCollectionHandler getMangaCollectionHandler,
        AddOwnedVolumeToCollectionHandler addOwnedVolumeToCollectionHandler,
        RemoveOwnedVolumeFromCollectionHandler removeOwnedVolumeFromCollectionHandler,
        GetMangaCollectionItemHandler getMangaCollectionItemHandler,
        UpdateCustomTotalVolumesHandler updateCustomTotalVolumesHandler,
        ICurrentUserService currentUserService)
    {
        _addMangaToCollectionHandler = addMangaToCollectionHandler;
        _getMangaCollectionHandler = getMangaCollectionHandler;
        _addOwnedVolumeToCollectionHandler = addOwnedVolumeToCollectionHandler;
        _removeOwnedVolumeFromCollectionHandler = removeOwnedVolumeFromCollectionHandler;
        _getMangaCollectionItemHandler = getMangaCollectionItemHandler;
        _updateCustomTotalVolumesHandler = updateCustomTotalVolumesHandler;
        _currentUserService = currentUserService;

    }

    [HttpGet]
    public async Task<ActionResult<GetMangaCollectionResult>> GetMangaCollection(
     CancellationToken cancellationToken)
    {
        var result = await _getMangaCollectionHandler.HandleAsync(_currentUserService.UserId, cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AddMangaToCollectionResult>> AddMangaToCollection(
        AddMangaToCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddMangaToCollectionCommand(
            UserId: _currentUserService.UserId,
            MalId: request.MalId,
            CustomTotalVolumes: request.CustomTotalVolumes);

        var result = await _addMangaToCollectionHandler.HandleAsync(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(AddMangaToCollection),
            new { id = result.Id },
            result);
    }

    [HttpPost("{id:guid}/volumes")]
    public async Task<ActionResult<AddOwnedVolumeToCollectionResult>> AddOwnedVolumeToCollection(
        Guid id,
        AddOwnedVolumeToCollectionRequest request,
        CancellationToken cancellationToken)
        {
            var command = new AddOwnedVolumeToCollectionCommand(
                UserId: _currentUserService.UserId,
                CollectionItemId: id,
                VolumeNumber: request.VolumeNumber,
                PurchaseDate: request.PurchaseDate,
                Price: request.Price,
                Store: request.Store);

            var result = await _addOwnedVolumeToCollectionHandler.HandleAsync(
                command,
                cancellationToken);

            return Ok(result);
        }

    [HttpDelete("{id:guid}/volumes/{volumeNumber:int}")]
    public async Task<ActionResult<RemoveOwnedVolumeFromCollectionResult>> RemoveOwnedVolumeFromCollection(
        Guid id,
        int volumeNumber,
        CancellationToken cancellationToken)
        {
            var command = new RemoveOwnedVolumeFromCollectionCommand(
                UserId: _currentUserService.UserId,
                CollectionItemId: id,
                VolumeNumber: volumeNumber);

            var result = await _removeOwnedVolumeFromCollectionHandler.HandleAsync(
                command,
                cancellationToken);

            return Ok(result);
        }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetMangaCollectionItemResult>> GetMangaCollectionItem(
        Guid id,
        CancellationToken cancellationToken)
        {
            var result = await _getMangaCollectionItemHandler.HandleAsync(
                _currentUserService.UserId,
                id,
                cancellationToken);

            return Ok(result);
        }
    

    [HttpPut("{id:guid}/total-volumes")]
    public async Task<ActionResult<UpdateCustomTotalVolumesResult>> UpdateCustomTotalVolumes(
        Guid id,
        UpdateCustomTotalVolumesRequest request,
        CancellationToken cancellationToken)
        {
            var command = new UpdateCustomTotalVolumesCommand(
                UserId: _currentUserService.UserId,
                CollectionItemId: id,
                TotalVolumes: request.TotalVolumes);

            var result = await _updateCustomTotalVolumesHandler.HandleAsync(
                command,
                cancellationToken);

            return Ok(result);
        }

 }
public sealed record AddMangaToCollectionRequest(
    int MalId,
    int? CustomTotalVolumes
);

public sealed record AddOwnedVolumeToCollectionRequest(
    int VolumeNumber,
    DateOnly? PurchaseDate,
    decimal? Price,
    string? Store
);

public sealed record UpdateCustomTotalVolumesRequest(
    int? TotalVolumes
);