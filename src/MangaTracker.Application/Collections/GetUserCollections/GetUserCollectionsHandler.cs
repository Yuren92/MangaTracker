using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.GetUserCollections;

public sealed class GetUserCollectionsHandler
{
    private readonly ICollectionQueries _collectionQueries;

    public GetUserCollectionsHandler(ICollectionQueries collectionQueries)
    {
        _collectionQueries = collectionQueries;
    }

    public async Task<GetUserCollectionsResult> HandleAsync(
        GetUserCollectionsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        var items = await _collectionQueries.GetCollectionSummariesAsync(
            query.UserId,
            cancellationToken);

        return new GetUserCollectionsResult(items);
    }
}
