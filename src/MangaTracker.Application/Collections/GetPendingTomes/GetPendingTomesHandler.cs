using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.GetPendingTomes;

public sealed class GetPendingTomesHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;

    public GetPendingTomesHandler(IUserCollectionRepository userCollectionRepository)
    {
        _userCollectionRepository = userCollectionRepository;
    }

    public async Task<GetPendingTomesResult> HandleAsync(
        GetPendingTomesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        var collections = await _userCollectionRepository.GetAllByUserIdAsync(
            query.UserId,
            cancellationToken);

        var items = collections
            .SelectMany(collection =>
            {
                var ownedTomeIds = collection.OwnedTomes
                    .Select(ownedTome => ownedTome.TomeId)
                    .ToHashSet();

                return collection.Edition.Tomes
                    .Where(tome => !ownedTomeIds.Contains(tome.Id))
                    .Select(tome => new PendingTomeResult(
                        CollectionId: collection.Id,
                        EditionId: collection.EditionId,
                        TomeId: tome.Id,
                        SeriesTitle: collection.Edition.Series.Title,
                        EditionName: collection.Edition.Name,
                        PublisherName: collection.Edition.PublisherName,
                        IssueNumber: tome.IssueNumber,
                        NormalizedNumber: tome.NormalizedNumber,
                        TomeTitle: tome.Title,
                        ImageUrl: tome.ImageUrl,
                        CoverDate: tome.CoverDate,
                        StoreDate: tome.StoreDate));
            })
            .OrderBy(item => item.SeriesTitle)
            .ThenBy(item => item.PublisherName)
            .ThenBy(item => item.NormalizedNumber ?? int.MaxValue)
            .ThenBy(item => item.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new GetPendingTomesResult(items);
    }
}