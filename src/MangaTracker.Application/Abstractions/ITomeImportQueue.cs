namespace MangaTracker.Application.Abstractions;

// Downloads an edition's tomes outside the HTTP request that added it: adding a series
// answers as soon as the edition exists, and its tomes follow a few seconds later.
// If the process stops with editions still queued, the daily catalog sync completes them
// (an edition with fewer tomes than Comic Vine lists counts as changed).
public interface ITomeImportQueue
{
    Task EnqueueAsync(Guid editionId, CancellationToken cancellationToken = default);

    // True while the edition's tomes are queued or being downloaded.
    bool IsPending(Guid editionId);
}
