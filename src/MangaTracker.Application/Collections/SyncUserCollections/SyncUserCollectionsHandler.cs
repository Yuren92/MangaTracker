using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Collections.SyncUserCollections;

public sealed class SyncUserCollectionsHandler
{
    private static readonly TimeSpan SyncCooldown = TimeSpan.FromHours(24);
    private const int MaxCollectionsPerSync = 5;

    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly ITomeRepository _tomeRepository;
    private readonly IComicVineClient _comicVineClient;

    public SyncUserCollectionsHandler(
        IUserCollectionRepository userCollectionRepository,
        ITomeRepository tomeRepository,
        IComicVineClient comicVineClient)
    {
        _userCollectionRepository = userCollectionRepository;
        _tomeRepository = tomeRepository;
        _comicVineClient = comicVineClient;
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

        var checkedCollections = collections.Count;
        var syncedCollections = 0;
        var skippedCollections = 0;
        var newTomes = 0;

        foreach (var collection in collections.Take(MaxCollectionsPerSync))
        {
            var edition = collection.Edition;

            if (!ShouldSync(edition))
            {
                skippedCollections++;
                continue;
            }

            var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
                edition.ComicVineApiDetailUrl,
                cancellationToken);

            if (volume is null)
            {
                skippedCollections++;
                continue;
            }

            edition.SyncDetails(
                name: volume.Name,
                publisherName: volume.PublisherName,
                startYear: volume.StartYear,
                description: volume.Description,
                imageUrl: volume.ImageUrl,
                siteDetailUrl: volume.SiteDetailUrl,
                issueCount: volume.CountOfIssues);

            var existingIssueUrls = edition.Tomes
                .Select(tome => tome.ComicVineApiDetailUrl)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newIssueSummaries = volume.Issues
                .Where(issue => !existingIssueUrls.Contains(issue.ApiDetailUrl))
                .OrderBy(issue => issue.NormalizedNumber ?? int.MaxValue)
                .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var issueSummary in newIssueSummaries)
            {
                var issueDetail = await _comicVineClient.GetIssueByApiDetailUrlAsync(
                    issueSummary.ApiDetailUrl,
                    cancellationToken);

                if (issueDetail is null)
                {
                    continue;
                }

                var existingTome = await _tomeRepository.GetByComicVineApiDetailUrlAsync(
                    issueDetail.ApiDetailUrl,
                    cancellationToken);

                if (existingTome is not null)
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

                newTomes++;
            }

            syncedCollections++;
        }

        await _userCollectionRepository.SaveChangesAsync(cancellationToken);

        return new SyncUserCollectionsResult(
            CheckedCollections: checkedCollections,
            SyncedCollections: syncedCollections,
            SkippedCollections: skippedCollections,
            NewTomes: newTomes);
    }

    private static bool ShouldSync(Edition edition)
    {
        if (edition.LastSyncedAt is null)
        {
            return true;
        }

        return DateTimeOffset.UtcNow - edition.LastSyncedAt.Value >= SyncCooldown;
    }
}