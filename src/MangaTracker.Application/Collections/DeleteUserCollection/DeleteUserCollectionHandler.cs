using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.DeleteUserCollection;

public sealed class DeleteUserCollectionHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCollectionHandler(
        IUserCollectionRepository userCollectionRepository,
        IUnitOfWork unitOfWork)
    {
        _userCollectionRepository = userCollectionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteUserCollectionResult> HandleAsync(
        DeleteUserCollectionCommand command,
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

        _userCollectionRepository.Remove(collection);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteUserCollectionResult(
            CollectionId: command.CollectionId,
            Deleted: true);
    }
}