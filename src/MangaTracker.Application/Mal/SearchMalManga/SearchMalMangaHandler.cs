using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Mal.SearchMalManga;

public sealed class SearchMalMangaHandler
{
    private const int MinSearchLength = 3;

    private readonly IMalMangaClient _malMangaClient;

    public SearchMalMangaHandler(IMalMangaClient malMangaClient)
    {
        _malMangaClient = malMangaClient;
    }

    public async Task<SearchMalMangaResult> HandleAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim();

        if (cleanQuery.Length < MinSearchLength)
        {
            throw new ValidationException($"Search query must have at least {MinSearchLength} characters.");
        }

        var items = await _malMangaClient.SearchMangaAsync(
            cleanQuery,
            limit,
            cancellationToken);

        return new SearchMalMangaResult(items);
    }
}