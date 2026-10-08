using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.EditionTomes;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace MangaTracker.Application.Catalog.SyncCatalog;

// Brings every collected edition up to date with Comic Vine, for all users at once:
// editions are shared, so there is no point in each user syncing them separately.
//
// 1. One request per 100 editions returns each volume's name, publisher, cover and
//    issue count. Its date_last_updated cannot be used: it does not change when an
//    issue is added (https://comicvine.gamespot.com/forums/api-developers-2334/).
// 2. Editions with more issues than stored tomes are the changed ones. Their new issues
//    come from one shared request, filtered by volume and by date_added since the
//    oldest of their last syncs (pages of 100 if there are more).
// 3. An edition that still lacks tomes after that (a partial import: its missing issues
//    are old, so the date filter skips them) downloads its full issue list.
// 4. Each edition is saved on its own, so a failure never loses the ones before it.
//
// A run with nothing new costs one request per 100 editions; with changes, usually one more.
public sealed class SyncCatalogHandler
{
    public const int VolumesPerRequest = 100;

    // Comic Vine's date_added is not documented with a time zone; a day of overlap
    // makes sure nothing falls between two runs. Already stored issues are skipped.
    private static readonly TimeSpan SinceMargin = TimeSpan.FromDays(1);

    private readonly IEditionRepository _editionRepository;
    private readonly IComicVineClient _comicVineClient;
    private readonly EditionTomeImporter _tomeImporter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncCatalogHandler> _logger;

    public SyncCatalogHandler(
        IEditionRepository editionRepository,
        IComicVineClient comicVineClient,
        EditionTomeImporter tomeImporter,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<SyncCatalogHandler> logger)
    {
        _editionRepository = editionRepository;
        _comicVineClient = comicVineClient;
        _tomeImporter = tomeImporter;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SyncCatalogResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _editionRepository.GetSyncCandidatesAsync(cancellationToken);
        var changed = new List<(EditionSyncCandidate Candidate, ComicVineVolumeSummaryDto Volume)>();
        ILookup<int, ComicVineIssueDetailDto> newIssues;

        try
        {
            foreach (var batch in candidates.Chunk(VolumesPerRequest))
            {
                var volumes = (await _comicVineClient.GetVolumeSummariesAsync(
                        batch.Select(candidate => candidate.ComicVineVolumeId).ToList(),
                        cancellationToken))
                    .ToDictionary(volume => volume.ComicVineVolumeId);

                changed.AddRange(batch
                    .Where(candidate =>
                        volumes.TryGetValue(candidate.ComicVineVolumeId, out var volume) &&
                        volume.CountOfIssues > candidate.StoredTomes)
                    .Select(candidate => (candidate, volumes[candidate.ComicVineVolumeId])));
            }

            newIssues = changed.Count == 0
                ? Enumerable.Empty<ComicVineIssueDetailDto>().ToLookup(issue => 0)
                : (await _comicVineClient.GetIssuesAddedSinceAsync(
                        changed.Select(item => item.Candidate.ComicVineVolumeId).ToList(),
                        changed.Min(item => item.Candidate.LastRefreshedAt) - SinceMargin,
                        cancellationToken))
                    .ToLookup(issue => issue.ComicVineVolumeId ?? 0);
        }
        catch (ExternalServiceUnavailableException exception)
        {
            // Nothing was written yet; the next run starts over.
            _logger.LogWarning(exception, "Comic Vine unavailable while checking {Editions} editions", candidates.Count);
            return new SyncCatalogResult(candidates.Count, changed.Count, 0, 0, 0, Completed: false);
        }

        var synced = 0;
        var failed = 0;
        var newTomes = 0;

        foreach (var (candidate, volume) in changed)
        {
            try
            {
                newTomes += await SyncEditionAsync(candidate, volume, newIssues[volume.ComicVineVolumeId], cancellationToken);
                synced++;
            }
            catch (ExternalServiceUnavailableException exception)
            {
                // Comic Vine itself is down: the remaining editions would fail the same
                // way. What was saved so far stays; the rest is retried on the next run.
                _logger.LogWarning(exception, "Comic Vine unavailable, catalog sync stopped after {Synced} editions", synced);
                _unitOfWork.DiscardChanges();
                failed++;

                return new SyncCatalogResult(candidates.Count, changed.Count, synced, failed, newTomes, Completed: false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Specific to this edition (bad data, a concurrent import of the same
                // tome...): skip it and keep going with the others.
                _logger.LogWarning(exception, "Catalog sync failed for edition {EditionId}", candidate.EditionId);
                _unitOfWork.DiscardChanges();
                failed++;
            }
        }

        _logger.LogInformation(
            "Catalog sync: {Checked} editions checked, {Changed} changed, {Synced} synced, {Failed} failed, {NewTomes} new tomes",
            candidates.Count, changed.Count, synced, failed, newTomes);

        return new SyncCatalogResult(candidates.Count, changed.Count, synced, failed, newTomes, Completed: true);
    }

    // Returns how many tomes were added.
    private async Task<int> SyncEditionAsync(
        EditionSyncCandidate candidate,
        ComicVineVolumeSummaryDto volume,
        IEnumerable<ComicVineIssueDetailDto> newIssues,
        CancellationToken cancellationToken)
    {
        var edition = await _editionRepository.GetByIdWithTomesAsync(candidate.EditionId, cancellationToken);

        if (edition is null)
        {
            // Removed since the candidates were read (its last collection was deleted).
            return 0;
        }

        // The batched list carries no description; the stored one is kept.
        edition.SyncDetails(
            name: volume.Name,
            publisherName: volume.PublisherName,
            startYear: volume.StartYear,
            description: edition.Description,
            imageUrl: volume.ImageUrl,
            siteDetailUrl: volume.SiteDetailUrl,
            issueCount: volume.CountOfIssues,
            syncedAt: _timeProvider.GetUtcNow());

        var storedBefore = edition.Tomes.Count;
        var added = await _tomeImporter.AddMissingTomesAsync(edition, newIssues, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (storedBefore + added < volume.CountOfIssues)
        {
            // Still short: the missing issues predate the date filter (a partial import).
            added += await _tomeImporter.ImportMissingTomesAsync(edition.Id, cancellationToken);
        }

        return added;
    }
}

// An edition someone collects, with the number of tomes stored for it and when it was
// last refreshed (its import date if it was never synced).
public sealed record EditionSyncCandidate(
    Guid EditionId,
    int ComicVineVolumeId,
    int StoredTomes,
    DateTimeOffset LastRefreshedAt);

/// <param name="CheckedEditions">Collected editions compared with Comic Vine.</param>
/// <param name="ChangedEditions">Editions with more issues in Comic Vine than tomes stored.</param>
/// <param name="SyncedEditions">Changed editions refreshed and saved.</param>
/// <param name="FailedEditions">Changed editions that could not be refreshed.</param>
/// <param name="NewTomes">Tomes added across all synced editions.</param>
/// <param name="Completed">False when Comic Vine became unavailable and the run stopped early.</param>
public sealed record SyncCatalogResult(
    int CheckedEditions,
    int ChangedEditions,
    int SyncedEditions,
    int FailedEditions,
    int NewTomes,
    bool Completed);
