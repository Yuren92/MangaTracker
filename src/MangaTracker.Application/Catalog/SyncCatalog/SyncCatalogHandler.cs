using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MangaTracker.Application.Catalog.SyncCatalog;

// Brings every collected edition up to date with Comic Vine, for all users at once:
// editions are shared, so there is no point in each user syncing them separately.
//
// 1. One request per 100 editions asks Comic Vine how many issues each volume has.
//    Its date_last_updated cannot be used for this: it does not change when an issue
//    is added (https://comicvine.gamespot.com/forums/api-developers-2334/).
// 2. Only editions with more issues than stored tomes are loaded and fetched (volume
//    plus one request per 100 issues). That also resumes partial imports.
// 3. Each edition is saved on its own, so a failure never loses the ones before it.
public sealed class SyncCatalogHandler
{
    public const int VolumesPerRequest = 100;

    private readonly IEditionRepository _editionRepository;
    private readonly ITomeRepository _tomeRepository;
    private readonly IComicVineClient _comicVineClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncCatalogHandler> _logger;

    public SyncCatalogHandler(
        IEditionRepository editionRepository,
        ITomeRepository tomeRepository,
        IComicVineClient comicVineClient,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<SyncCatalogHandler> logger)
    {
        _editionRepository = editionRepository;
        _tomeRepository = tomeRepository;
        _comicVineClient = comicVineClient;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<SyncCatalogResult> HandleAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _editionRepository.GetSyncCandidatesAsync(cancellationToken);
        var changed = new List<EditionSyncCandidate>();

        try
        {
            foreach (var batch in candidates.Chunk(VolumesPerRequest))
            {
                var counts = await _comicVineClient.GetVolumeIssueCountsAsync(
                    batch.Select(candidate => candidate.ComicVineVolumeId).ToList(),
                    cancellationToken);

                changed.AddRange(batch.Where(candidate =>
                    counts.TryGetValue(candidate.ComicVineVolumeId, out var issueCount) &&
                    issueCount > candidate.StoredTomes));
            }
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

        foreach (var candidate in changed)
        {
            try
            {
                var added = await SyncEditionAsync(candidate.EditionId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                newTomes += added;
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
    private async Task<int> SyncEditionAsync(Guid editionId, CancellationToken cancellationToken)
    {
        var edition = await _editionRepository.GetByIdWithTomesAsync(editionId, cancellationToken);

        if (edition is null)
        {
            // Removed since the candidates were read (its last collection was deleted).
            return 0;
        }

        var now = _timeProvider.GetUtcNow();

        var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
            edition.ComicVineApiDetailUrl,
            cancellationToken);

        if (volume is null)
        {
            edition.MarkSyncAttempted(now);
            return 0;
        }

        edition.SyncDetails(
            name: volume.Name,
            publisherName: volume.PublisherName,
            startYear: volume.StartYear,
            description: volume.Description,
            imageUrl: volume.ImageUrl,
            siteDetailUrl: volume.SiteDetailUrl,
            issueCount: volume.CountOfIssues,
            syncedAt: now);

        var existingIssueUrls = edition.Tomes
            .Select(tome => tome.ComicVineApiDetailUrl)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newIssueSummaries = volume.Issues
            .Where(issue => !existingIssueUrls.Contains(issue.ApiDetailUrl))
            .ToList();

        if (newIssueSummaries.Count == 0)
        {
            return 0;
        }

        // Tomes for these issues may already exist (e.g. stored under another edition);
        // fetch them all at once rather than with one query per issue.
        var storedTomesByUrl = (await _tomeRepository.GetByComicVineApiDetailUrlsAsync(
                newIssueSummaries.Select(issue => issue.ApiDetailUrl).ToList(),
                cancellationToken))
            .ToDictionary(tome => tome.ComicVineApiDetailUrl, StringComparer.OrdinalIgnoreCase);

        // The details of every issue of the volume, one request per 100 issues.
        var issueDetails = (await _comicVineClient.GetVolumeIssuesAsync(volume.ComicVineVolumeId, cancellationToken))
            .DistinctBy(issue => issue.ApiDetailUrl, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(issue => issue.ApiDetailUrl, StringComparer.OrdinalIgnoreCase);

        var addedTomes = 0;

        foreach (var issueSummary in newIssueSummaries)
        {
            if (!issueDetails.TryGetValue(issueSummary.ApiDetailUrl, out var issueDetail))
            {
                continue;
            }

            if (storedTomesByUrl.TryGetValue(issueDetail.ApiDetailUrl, out var existingTome))
            {
                existingTome.SyncDetails(
                    issueNumber: issueDetail.IssueNumber,
                    normalizedNumber: issueDetail.NormalizedNumber,
                    title: issueDetail.Title,
                    imageUrl: issueDetail.ImageUrl,
                    coverDate: issueDetail.CoverDate,
                    storeDate: issueDetail.StoreDate,
                    siteDetailUrl: issueDetail.SiteDetailUrl);

                continue;
            }

            await _tomeRepository.AddAsync(
                new Tome(
                    editionId: edition.Id,
                    comicVineIssueId: issueDetail.ComicVineIssueId,
                    comicVineApiDetailUrl: issueDetail.ApiDetailUrl,
                    issueNumber: issueDetail.IssueNumber,
                    normalizedNumber: issueDetail.NormalizedNumber,
                    title: issueDetail.Title,
                    imageUrl: issueDetail.ImageUrl,
                    coverDate: issueDetail.CoverDate,
                    storeDate: issueDetail.StoreDate,
                    siteDetailUrl: issueDetail.SiteDetailUrl),
                cancellationToken);

            addedTomes++;
        }

        return addedTomes;
    }
}

// An edition someone collects, with the number of tomes stored for it.
public sealed record EditionSyncCandidate(
    Guid EditionId,
    int ComicVineVolumeId,
    int StoredTomes);

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
