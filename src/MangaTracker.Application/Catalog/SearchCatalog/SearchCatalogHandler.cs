using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Catalog.SearchCatalog;

public sealed class SearchCatalogHandler
{
    private const int MaxQueryLength = 100;
    private const int MaxLimit = 50;

    private readonly IComicVineClient _comicVineClient;

    public SearchCatalogHandler(IComicVineClient comicVineClient)
    {
        _comicVineClient = comicVineClient;
    }

    public Task<IReadOnlyCollection<ComicVineVolumeSearchResultDto>> HandleAsync(
        SearchCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        var text = query.Query?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException("Search query is required.");
        }

        // Every search costs a call against the Comic Vine quota, so input is bounded
        // here instead of trusting whatever the client sends.
        if (text.Length > MaxQueryLength)
        {
            throw new ValidationException($"Search query must have at most {MaxQueryLength} characters.");
        }

        var limit = Math.Clamp(query.Limit, 1, MaxLimit);

        return _comicVineClient.SearchVolumesAsync(text, limit, cancellationToken);
    }
}
