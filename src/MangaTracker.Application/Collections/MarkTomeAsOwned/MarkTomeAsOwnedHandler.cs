using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.MarkTomeAsOwned;

public sealed class MarkTomeAsOwnedHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly ITomeRepository _tomeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkTomeAsOwnedHandler(
        IUserCollectionRepository userCollectionRepository,
        ITomeRepository tomeRepository,
        IUnitOfWork unitOfWork)
    {
        _userCollectionRepository = userCollectionRepository;
        _tomeRepository = tomeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MarkTomeAsOwnedResult> HandleAsync(
        MarkTomeAsOwnedCommand command,
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

        var tome = await _tomeRepository.GetByIdAsync(command.TomeId, cancellationToken)
            ?? throw new NotFoundException("Tome was not found.");

        // The domain rejects tomes from another edition (400).
        collection.MarkTomeAsOwned(tome);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var totalTomes = collection.Edition.Tomes.Count;
        var ownedTomes = collection.OwnedTomes.Count;
        var pendingTomes = totalTomes - ownedTomes;

        return new MarkTomeAsOwnedResult(
            CollectionId: collection.Id,
            TomeId: command.TomeId,
            IsOwned: true,
            TotalTomes: totalTomes,
            OwnedTomes: ownedTomes,
            PendingTomes: pendingTomes);
    }
}