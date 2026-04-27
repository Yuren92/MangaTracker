using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed class AddMangaToCollectionHandler
{
    private readonly IMangaCollectionRepository _repository;
    private readonly IMalMangaClient _malMangaClient;

    public AddMangaToCollectionHandler(
        IMangaCollectionRepository repository,
        IMalMangaClient malMangaClient)
    {
        _repository = repository;
        _malMangaClient = malMangaClient;
    }

    public async Task<AddMangaToCollectionResult> HandleAsync(
        AddMangaToCollectionCommand command,
        CancellationToken cancellationToken = default)
    {
        var existingManga = await _repository.GetByUserIdAndMalIdAsync(
            command.UserId,
            command.MalId,
            cancellationToken);

        if (existingManga is not null)
        {
            throw new ConflictException("Manga is already in collection.");
        }

        var malManga = await _malMangaClient.GetMangaDetailAsync(
            command.MalId,
            cancellationToken);

        if (malManga is null)
        {
            throw new NotFoundException("Manga was not found in MyAnimeList.");
        }

        var manga = new MangaCollectionItem(
            userId: command.UserId,
            malId: malManga.MalId,
            title: malManga.Title,
            imageUrl: malManga.ImageUrl,
            malTotalVolumes: malManga.TotalVolumes,
            customTotalVolumes: command.CustomTotalVolumes);

        await _repository.AddAsync(manga, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new AddMangaToCollectionResult(
            Id: manga.Id,
            UserId: manga.UserId,
            MalId: manga.MalId,
            Title: manga.Title,
            ImageUrl: manga.ImageUrl,
            EffectiveTotalVolumes: manga.EffectiveTotalVolumes);
        }
}