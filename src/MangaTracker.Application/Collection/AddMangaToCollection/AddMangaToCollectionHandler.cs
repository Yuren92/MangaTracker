using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Common;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Collection.AddMangaToCollection;

public sealed class AddMangaToCollectionHandler
{
    private readonly IMangaCollectionRepository _repository;

    public AddMangaToCollectionHandler(IMangaCollectionRepository repository)
    {
        _repository = repository;
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
            throw new DomainException("Manga is already in collection.");
        }

        var manga = new MangaCollectionItem(
            userId: command.UserId,
            malId: command.MalId,
            title: command.Title,
            imageUrl: command.ImageUrl,
            malTotalVolumes: command.MalTotalVolumes,
            customTotalVolumes: command.CustomTotalVolumes);

        await _repository.AddAsync(manga, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new AddMangaToCollectionResult(
            UserId: manga.UserId,
            Id: manga.Id,
            MalId: manga.MalId,
            Title: manga.Title,
            ImageUrl: manga.ImageUrl,
            EffectiveTotalVolumes: manga.EffectiveTotalVolumes);
    }
}