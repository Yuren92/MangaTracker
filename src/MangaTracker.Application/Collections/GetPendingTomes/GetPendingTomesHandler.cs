using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collections.GetPendingTomes;

public sealed class GetPendingTomesHandler
{
    private readonly ICollectionQueries _collectionQueries;

    public GetPendingTomesHandler(ICollectionQueries collectionQueries)
    {
        _collectionQueries = collectionQueries;
    }

    public async Task<GetPendingTomesResult> HandleAsync(
        GetPendingTomesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        var items = await _collectionQueries.GetPendingTomesAsync(
            query.UserId,
            cancellationToken);

        return new GetPendingTomesResult(items);
    }
}