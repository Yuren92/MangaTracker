using MangaTracker.Application.Collection.AddMangaToCollection;
using Microsoft.AspNetCore.Mvc;

namespace MangaTracker.Api.Controllers;

[ApiController]
[Route("api/collection")]
public sealed class CollectionController : ControllerBase
{
    private readonly AddMangaToCollectionHandler _addMangaToCollectionHandler;

    public CollectionController(AddMangaToCollectionHandler addMangaToCollectionHandler)
    {
        _addMangaToCollectionHandler = addMangaToCollectionHandler;
    }

    [HttpPost]
    public async Task<ActionResult<AddMangaToCollectionResult>> AddMangaToCollection(
        AddMangaToCollectionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddMangaToCollectionCommand(
            MalId: request.MalId,
            Title: request.Title,
            ImageUrl: request.ImageUrl,
            MalTotalVolumes: request.MalTotalVolumes,
            CustomTotalVolumes: request.CustomTotalVolumes);

        var result = await _addMangaToCollectionHandler.HandleAsync(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(AddMangaToCollection),
            new { id = result.Id },
            result);
    }
}

public sealed record AddMangaToCollectionRequest(
    int MalId,
    string Title,
    string? ImageUrl,
    int? MalTotalVolumes,
    int? CustomTotalVolumes
);