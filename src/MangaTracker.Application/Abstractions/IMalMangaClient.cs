using MangaTracker.Application.Mal.Dtos;

namespace MangaTracker.Application.Abstractions;

public interface IMalMangaClient
{
    Task<IReadOnlyCollection<MalMangaSearchResultDto>> SearchMangaAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    Task<MalMangaDetailDto?> GetMangaDetailAsync(
        int malId,
        CancellationToken cancellationToken = default);
}