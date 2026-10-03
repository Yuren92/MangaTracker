using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.UnmarkTomeAsOwned;

public sealed class UnmarkTomeAsOwnedHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnmarkTomeAsOwnedHandler(
        IUserCollectionRepository userCollectionRepository,
        IUnitOfWork unitOfWork)
    {
        _userCollectionRepository = userCollectionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UnmarkTomeAsOwnedResult> HandleAsync(
        UnmarkTomeAsOwnedCommand command,
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

        if (command.TomeId == Guid.Empty)
        {
            throw new ValidationException("Tome id is required.");
        }

        var collection = await _userCollectionRepository.GetByUserIdAndIdAsync(
            command.UserId,
            command.CollectionId,
            cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        var tomeBelongsToCollection = collection.Edition.Tomes.Any(tome =>
            tome.Id == command.TomeId);

        if (!tomeBelongsToCollection)
        {
            throw new ValidationException("Tome does not belong to this collection.");
        }

        collection.UnmarkTomeAsOwned(command.TomeId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var totalTomes = collection.Edition.Tomes.Count;
        var ownedTomes = collection.OwnedTomes.Count;
        var pendingTomes = totalTomes - ownedTomes;

        return new UnmarkTomeAsOwnedResult(
            CollectionId: collection.Id,
            TomeId: command.TomeId,
            IsOwned: false,
            TotalTomes: totalTomes,
            OwnedTomes: ownedTomes,
            PendingTomes: pendingTomes);
    }
}