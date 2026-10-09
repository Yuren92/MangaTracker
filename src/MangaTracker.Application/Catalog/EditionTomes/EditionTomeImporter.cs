using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Catalog.EditionTomes;

// Turns Comic Vine issues into tomes of an edition. Shared by the background import of
// a newly added series and by the catalog sync, so both add tomes the same way.
public sealed class EditionTomeImporter
{
    private readonly IEditionRepository _editionRepository;
    private readonly ITomeRepository _tomeRepository;
    private readonly IComicVineClient _comicVineClient;
    private readonly IUnitOfWork _unitOfWork;

    public EditionTomeImporter(
        IEditionRepository editionRepository,
        ITomeRepository tomeRepository,
        IComicVineClient comicVineClient,
        IUnitOfWork unitOfWork)
    {
        _editionRepository = editionRepository;
        _tomeRepository = tomeRepository;
        _comicVineClient = comicVineClient;
        _unitOfWork = unitOfWork;
    }

    // Fetches every issue of the edition's volume (one request per 100) and stores the
    // ones without a tome. Running it twice adds nothing; returns how many were added.
    public async Task<int> ImportMissingTomesAsync(Guid editionId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ImportOnceAsync(editionId, cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // Another import of the same edition stored some of these tomes first. Its
            // rows are committed now: a second pass from a clean state skips them.
            _unitOfWork.DiscardChanges();

            return await ImportOnceAsync(editionId, cancellationToken);
        }
    }

    // Stages the issues that are not tomes of the edition yet (the caller saves).
    // Issues stored under another edition only get their details refreshed: a Comic Vine
    // issue is one tome, whichever edition it was first imported with.
    public async Task<int> AddMissingTomesAsync(
        Edition edition,
        IEnumerable<ComicVineIssueDetailDto> issues,
        CancellationToken cancellationToken = default)
    {
        var storedUrls = edition.Tomes
            .Select(tome => tome.ComicVineApiDetailUrl)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newIssues = issues
            .DistinctBy(issue => issue.ApiDetailUrl, StringComparer.OrdinalIgnoreCase)
            .Where(issue => !storedUrls.Contains(issue.ApiDetailUrl))
            .ToList();

        if (newIssues.Count == 0)
        {
            return 0;
        }

        var storedElsewhere = (await _tomeRepository.GetByComicVineApiDetailUrlsAsync(
                newIssues.Select(issue => issue.ApiDetailUrl).ToList(),
                cancellationToken))
            .ToDictionary(tome => tome.ComicVineApiDetailUrl, StringComparer.OrdinalIgnoreCase);

        var added = 0;

        foreach (var issue in newIssues)
        {
            if (storedElsewhere.TryGetValue(issue.ApiDetailUrl, out var existingTome))
            {
                existingTome.SyncDetails(
                    issueNumber: issue.IssueNumber,
                    normalizedNumber: issue.NormalizedNumber,
                    title: issue.Title,
                    imageUrl: issue.ImageUrl,
                    coverDate: issue.CoverDate,
                    storeDate: issue.StoreDate,
                    siteDetailUrl: issue.SiteDetailUrl);

                continue;
            }

            await _tomeRepository.AddAsync(
                new Tome(
                    editionId: edition.Id,
                    comicVineIssueId: issue.ComicVineIssueId,
                    comicVineApiDetailUrl: issue.ApiDetailUrl,
                    issueNumber: issue.IssueNumber,
                    normalizedNumber: issue.NormalizedNumber,
                    title: issue.Title,
                    imageUrl: issue.ImageUrl,
                    coverDate: issue.CoverDate,
                    storeDate: issue.StoreDate,
                    siteDetailUrl: issue.SiteDetailUrl),
                cancellationToken);

            added++;
        }

        return added;
    }

    private async Task<int> ImportOnceAsync(Guid editionId, CancellationToken cancellationToken)
    {
        var edition = await _editionRepository.GetByIdWithTomesAsync(editionId, cancellationToken);

        if (edition is null)
        {
            // Removed while it was queued (its last collection was deleted).
            return 0;
        }

        var issues = await _comicVineClient.GetVolumeIssuesAsync(edition.ComicVineVolumeId, cancellationToken);
        var added = await AddMissingTomesAsync(edition, issues, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return added;
    }
}
