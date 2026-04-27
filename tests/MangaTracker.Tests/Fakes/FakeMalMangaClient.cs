using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Mal.Dtos;

namespace MangaTracker.Tests.Fakes;

public sealed class FakeMalMangaClient : IMalMangaClient
{
    private readonly Dictionary<int, MalMangaDetailDto> _mangas = [];

    public void AddManga(MalMangaDetailDto manga)
    {
        _mangas[manga.MalId] = manga;
    }

    public Task<IReadOnlyCollection<MalMangaSearchResultDto>> SearchMangaAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var cleanQuery = query.Trim();

        var results = _mangas.Values
            .Where(manga => manga.Title.Contains(
                cleanQuery,
                StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .Select(manga => new MalMangaSearchResultDto(
                MalId: manga.MalId,
                Title: manga.Title,
                ImageUrl: manga.ImageUrl))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<MalMangaSearchResultDto>>(results);
    }

    public Task<MalMangaDetailDto?> GetMangaDetailAsync(
        int malId,
        CancellationToken cancellationToken = default)
    {
        _mangas.TryGetValue(malId, out var manga);

        return Task.FromResult(manga);
    }
}