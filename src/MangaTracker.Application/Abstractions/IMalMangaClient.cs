using MangaTracker.Application.Mal.Dtos;

namespace MangaTracker.Application.Abstractions;

public interface IMalMangaClient
{
    Task<MalMangaDetailDto?> GetMangaDetailAsync(
        int malId,
        CancellationToken cancellationToken = default);
}