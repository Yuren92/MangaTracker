using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Mal.Dtos;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Mal.GetMalMangaDetail;

public sealed class GetMalMangaDetailHandler
{
    private readonly IMalMangaClient _malMangaClient;
    private readonly IMangaCollectionRepository _mangaCollectionRepository;

    public GetMalMangaDetailHandler(
        IMalMangaClient malMangaClient,
        IMangaCollectionRepository mangaCollectionRepository)
    {
        _malMangaClient = malMangaClient;
        _mangaCollectionRepository = mangaCollectionRepository;
    }

    public async Task<GetMalMangaDetailResult> HandleAsync(
        Guid userId,
        int malId,
        CancellationToken cancellationToken = default)
    {
        var manga = await _malMangaClient.GetMangaDetailAsync(
            malId,
            cancellationToken);

        if (manga is null)
        {
            throw new NotFoundException("Manga was not found in MyAnimeList.");
        }

        var collectionItem = await _mangaCollectionRepository.GetByUserIdAndMalIdAsync(
            userId,
            malId,
            cancellationToken);

        var item = new MalMangaDetailWithCollectionStatusDto(
            MalId: manga.MalId,
            Title: manga.Title,
            ImageUrl: manga.ImageUrl,
            TotalVolumes: manga.TotalVolumes,
            TotalChapters: manga.TotalChapters,
            Status: manga.Status,
            Synopsis: manga.Synopsis,
            Recommendations: manga.Recommendations,
            IsInCollection: collectionItem is not null,
            CollectionItemId: collectionItem?.Id);

        return new GetMalMangaDetailResult(item);
    }
}