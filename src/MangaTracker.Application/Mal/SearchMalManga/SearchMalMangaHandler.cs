using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Mal.SearchMalManga;

public sealed class SearchMalMangaHandler
{
    private const int MinSearchLength = 3;
    private const int DefaultLimit = 10;
    private const int MaxLimit = 20;

    private readonly IMalMangaClient _malMangaClient;

    public SearchMalMangaHandler(IMalMangaClient malMangaClient)
    {
        _malMangaClient = malMangaClient;
    }

    public async Task<SearchMalMangaResult> HandleAsync(
        string query,
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim();

        if (cleanQuery.Length < MinSearchLength)
        {
            throw new ValidationException($"Search query must have at least {MinSearchLength} characters.");
        }

        var normalizedLimit = NormalizeLimit(limit);

        var items = await _malMangaClient.SearchMangaAsync(
            cleanQuery,
            normalizedLimit,
            cancellationToken);

        return new SearchMalMangaResult(items);
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0)
        {
            return DefaultLimit;
        }

        return Math.Min(limit, MaxLimit);
    }
}