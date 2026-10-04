using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Catalog.PreviewComicVineVolume;

public sealed class PreviewComicVineVolumeHandler
{
    private readonly IComicVineClient _comicVineClient;

    public PreviewComicVineVolumeHandler(IComicVineClient comicVineClient)
    {
        _comicVineClient = comicVineClient;
    }

    public async Task<ComicVineVolumeDetailDto> HandleAsync(
        PreviewComicVineVolumeQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ApiDetailUrl))
        {
            throw new ValidationException("Comic Vine volume API detail URL is required.");
        }

        var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
            query.ApiDetailUrl,
            cancellationToken);

        return volume ?? throw new NotFoundException("Comic Vine volume was not found.");
    }
}
