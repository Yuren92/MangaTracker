using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace MangaTracker.Application.Collections.SyncUserCollections;

public sealed class SyncUserCollectionsHandler
{
    private static readonly TimeSpan SyncCooldown = TimeSpan.FromHours(24);
    private const int MaxCollectionsPerSync = 5;

    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly ITomeRepository _tomeRepository;
    private readonly IComicVineClient _comicVineClient;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SyncUserCollectionsHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public SyncUserCollectionsHandler(
        IUserCollectionRepository userCollectionRepository,
        ITomeRepository tomeRepository,
        IComicVineClient comicVineClient,
        TimeProvider timeProvider,
        ILogger<SyncUserCollectionsHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _userCollectionRepository = userCollectionRepository;
        _tomeRepository = tomeRepository;
        _comicVineClient = comicVineClient;
        _timeProvider = timeProvider;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<SyncUserCollectionsResult> HandleAsync(
        SyncUserCollectionsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        var collections = await _userCollectionRepository.GetAllByUserIdAsync(
            command.UserId,
            cancellationToken);

        var now = _timeProvider.GetUtcNow();

        // Pick due collections first and then cap them, oldest first. Capping before
        // filtering would keep looking at the same first collections and never reach
        // the rest once those were inside their cooldown.
        var dueCollections = collections
            .Where(collection => IsDue(collection.Edition, now))
            .OrderBy(collection => collection.Edition.LastRefreshedAt)
            .ToList();

        var collectionsToSync = dueCollections
            .Take(MaxCollectionsPerSync)
            .ToList();

        var syncedCollections = 0;
        var failedCollections = 0;
        var newTomes = 0;

        foreach (var collection in collectionsToSync)
        {
            var edition = collection.Edition;

            try
            {
                var addedTomes = await SyncEditionAsync(edition, now, cancellationToken);

                if (addedTomes is null)
                {
                    failedCollections++;
                    continue;
                }

                newTomes += addedTomes.Value;
                syncedCollections++;
            }
            catch (ExternalServiceUnavailableException exception)
            {
                // Comic Vine itself is down (after retries) rather than this volume being
                // broken. The rest would fail the same way, so stop here, keep what was
                // synced so far, and leave this edition due so it is retried next time.
                _logger.LogWarning(
                    exception,
                    "Comic Vine unavailable, stopping sync after {SyncedCollections} collections",
                    syncedCollections);

                failedCollections++;
                break;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // Anything specific to this edition must not abort the whole sync or keep
                // it first in the queue forever.
                _logger.LogWarning(
                    exception,
                    "Comic Vine sync failed for edition {EditionId}",
                    edition.Id);

                edition.MarkSyncAttempted(now);
                failedCollections++;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SyncUserCollectionsResult(
            CheckedCollections: collections.Count,
            SyncedCollections: syncedCollections,
            SkippedCollections: collections.Count - dueCollections.Count,
            DeferredCollections: dueCollections.Count - collectionsToSync.Count,
            FailedCollections: failedCollections,
            NewTomes: newTomes);
    }

    // Returns the number of new tomes, or null when Comic Vine no longer has the volume.
    private async Task<int?> SyncEditionAsync(
        Edition edition,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
            edition.ComicVineApiDetailUrl,
            cancellationToken);

        if (volume is null)
        {
            edition.MarkSyncAttempted(now);
            return null;
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
            .OrderBy(issue => issue.NormalizedNumber ?? int.MaxValue)
            .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (newIssueSummaries.Count == 0)
        {
            return 0;
        }

        // Tomes for these issues may already exist (e.g. stored under another edition);
        // fetch them all at once rather than with one query per issue.
        var storedTomes = await _tomeRepository.GetByComicVineApiDetailUrlsAsync(
            newIssueSummaries.Select(issue => issue.ApiDetailUrl).ToList(),
            cancellationToken);

        var storedTomesByUrl = storedTomes.ToDictionary(
            tome => tome.ComicVineApiDetailUrl,
            StringComparer.OrdinalIgnoreCase);

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

            var tome = new Tome(
                editionId: edition.Id,
                comicVineIssueId: issueDetail.ComicVineIssueId,
                comicVineApiDetailUrl: issueDetail.ApiDetailUrl,
                issueNumber: issueDetail.IssueNumber,
                normalizedNumber: issueDetail.NormalizedNumber,
                title: issueDetail.Title,
                imageUrl: issueDetail.ImageUrl,
                coverDate: issueDetail.CoverDate,
                storeDate: issueDetail.StoreDate,
                siteDetailUrl: issueDetail.SiteDetailUrl);

            await _tomeRepository.AddAsync(tome, cancellationToken);

            addedTomes++;
        }

        return addedTomes;
    }

    private static bool IsDue(Edition edition, DateTimeOffset now)
    {
        return now - edition.LastRefreshedAt >= SyncCooldown;
    }
}
