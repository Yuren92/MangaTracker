using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.GetUserCollectionDetail;

public sealed class GetUserCollectionDetailHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;

    public GetUserCollectionDetailHandler(IUserCollectionRepository userCollectionRepository)
    {
        _userCollectionRepository = userCollectionRepository;
    }

    public async Task<GetUserCollectionDetailResult> HandleAsync(
        GetUserCollectionDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        if (query.CollectionId == Guid.Empty)
        {
            throw new ValidationException("Collection id is required.");
        }

        var collection = await _userCollectionRepository.GetByUserIdAndIdAsync(
            query.UserId,
            query.CollectionId,
            cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        var ownedTomeIds = collection.OwnedTomes
            .Select(ownedTome => ownedTome.TomeId)
            .ToHashSet();

        var tomes = collection.Edition.Tomes
            .OrderBy(tome => tome.NormalizedNumber ?? int.MaxValue)
            .ThenBy(tome => tome.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .Select(tome => new TomeDetailResult(
                TomeId: tome.Id,
                ComicVineIssueId: tome.ComicVineIssueId,
                IssueNumber: tome.IssueNumber,
                NormalizedNumber: tome.NormalizedNumber,
                Title: tome.Title,
                ImageUrl: tome.ImageUrl,
                CoverDate: tome.CoverDate,
                StoreDate: tome.StoreDate,
                IsOwned: ownedTomeIds.Contains(tome.Id)))
            .ToList();

        var totalTomes = tomes.Count;
        var ownedTomes = tomes.Count(tome => tome.IsOwned);
        var pendingTomes = totalTomes - ownedTomes;

        return new GetUserCollectionDetailResult(
            Id: collection.Id,
            EditionId: collection.EditionId,
            Title: collection.Edition.Series.Title,
            PublisherName: collection.Edition.PublisherName,
            ImageUrl: collection.Edition.ImageUrl,
            TotalTomes: totalTomes,
            OwnedTomes: ownedTomes,
            PendingTomes: pendingTomes,
            Tomes: tomes);
    }
}