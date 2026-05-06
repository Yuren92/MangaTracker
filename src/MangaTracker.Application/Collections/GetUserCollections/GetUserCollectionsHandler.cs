using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.GetUserCollections;

public sealed class GetUserCollectionsHandler
{
    private readonly IUserCollectionRepository _userCollectionRepository;

    public GetUserCollectionsHandler(IUserCollectionRepository userCollectionRepository)
    {
        _userCollectionRepository = userCollectionRepository;
    }

    public async Task<GetUserCollectionsResult> HandleAsync(
        GetUserCollectionsQuery query,
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
            .OrderBy(collection => collection.Edition.Series.Title)
            .ThenBy(collection => collection.Edition.PublisherName)
            .Select(collection =>
            {
                var totalTomes = collection.Edition.Tomes.Count;
                var ownedTomes = collection.OwnedTomes.Count;
                var pendingTomes = totalTomes - ownedTomes;

                return new UserCollectionListItemResult(
                    Id: collection.Id,
                    EditionId: collection.EditionId,
                    ComicVineVolumeId: collection.Edition.ComicVineVolumeId,
                    ComicVineApiDetailUrl: collection.Edition.ComicVineApiDetailUrl,
                    Title: collection.Edition.Series.Title,
                    PublisherName: collection.Edition.PublisherName,
                    ImageUrl: collection.Edition.ImageUrl,
                    TotalTomes: totalTomes,
                    OwnedTomes: ownedTomes,
                    PendingTomes: pendingTomes);
            })
            .ToList();

        return new GetUserCollectionsResult(items);
    }
}