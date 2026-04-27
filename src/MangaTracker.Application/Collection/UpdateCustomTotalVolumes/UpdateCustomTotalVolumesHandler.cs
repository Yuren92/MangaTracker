using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Collection.UpdateCustomTotalVolumes;

public sealed class UpdateCustomTotalVolumesHandler
{
    private readonly IMangaCollectionRepository _repository;

    public UpdateCustomTotalVolumesHandler(IMangaCollectionRepository repository)
    {
        _repository = repository;
    }

    public async Task<UpdateCustomTotalVolumesResult> HandleAsync(
        UpdateCustomTotalVolumesCommand command,
        CancellationToken cancellationToken = default)
    {
        var manga = await _repository.GetByUserIdAndIdAsync(
            command.UserId,
            command.CollectionItemId,
            cancellationToken);

        if (manga is null)
        {
            throw new NotFoundException("Manga collection item was not found.");
        }

        manga.SetCustomTotalVolumes(command.TotalVolumes);

        await _repository.SaveChangesAsync(cancellationToken);

        return new UpdateCustomTotalVolumesResult(
            CollectionItemId: manga.Id,
            CustomTotalVolumes: manga.CustomTotalVolumes,
            EffectiveTotalVolumes: manga.EffectiveTotalVolumes);
    }
}