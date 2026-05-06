using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.MarkAllTomesAsOwned;

public sealed class MarkAllTomesAsOwnedHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;

    public MarkAllTomesAsOwnedHandler(IUserCollectionRepository userCollectionRepository)
    {
        _userCollectionRepository = userCollectionRepository;
    }

    public async Task<MarkAllTomesAsOwnedResult> HandleAsync(
        MarkAllTomesAsOwnedCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        if (command.CollectionId == Guid.Empty)
        {
            throw new ValidationException("Collection id is required.");
        }

        var collection = await _userCollectionRepository.GetByUserIdAndIdAsync(
            command.UserId,
            command.CollectionId,
            cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        foreach (var tome in collection.Edition.Tomes)
        {
            collection.MarkTomeAsOwned(tome.Id);
        }

        await _userCollectionRepository.SaveChangesAsync(cancellationToken);

        var totalTomes = collection.Edition.Tomes.Count;
        var ownedTomes = collection.OwnedTomes.Count;
        var pendingTomes = totalTomes - ownedTomes;

        return new MarkAllTomesAsOwnedResult(
            CollectionId: collection.Id,
            TotalTomes: totalTomes,
            OwnedTomes: ownedTomes,
            PendingTomes: pendingTomes);
    }
}