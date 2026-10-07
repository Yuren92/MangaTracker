using MangaTracker.Application.Abstractions;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Collections.ImportComicVineVolume;

public sealed class ImportComicVineVolumeHandler
{
    private const int MaxIssuesPerImport = 250;

    private readonly IComicVineClient _comicVineClient;
    private readonly ISeriesRepository _seriesRepository;
    private readonly IEditionRepository _editionRepository;
    private readonly ITomeRepository _tomeRepository;
    private readonly IUserCollectionRepository _userCollectionRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ImportComicVineVolumeHandler(
        IComicVineClient comicVineClient,
        ISeriesRepository seriesRepository,
        IEditionRepository editionRepository,
        ITomeRepository tomeRepository,
        IUserCollectionRepository userCollectionRepository,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _comicVineClient = comicVineClient;
        _seriesRepository = seriesRepository;
        _editionRepository = editionRepository;
        _tomeRepository = tomeRepository;
        _userCollectionRepository = userCollectionRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportComicVineVolumeResult> HandleAsync(
        ImportComicVineVolumeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
        {
            throw new ValidationException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ApiDetailUrl))
        {
            throw new ValidationException("Comic Vine volume API detail URL is required.");
        }

        try
        {
            return await ImportAsync(command, cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            // A concurrent request imported the same volume (or created this user's
            // collection) between our reads and our save. Its rows are committed now, so
            // a second pass from a clean state finds and reuses them instead of inserting
            // duplicates. If it conflicts again, the 409 reaches the client.
            _unitOfWork.DiscardChanges();

            return await ImportAsync(command, cancellationToken);
        }
    }

    private async Task<ImportComicVineVolumeResult> ImportAsync(
        ImportComicVineVolumeCommand command,
        CancellationToken cancellationToken)
    {
        var existingEdition = await _editionRepository.GetByComicVineApiDetailUrlAsync(
            command.ApiDetailUrl,
            cancellationToken);

        if (existingEdition is not null)
        {
            var storedTomes = await _tomeRepository.GetByEditionIdAsync(
                existingEdition.Id,
                cancellationToken);

            // Fast path: the edition is already fully imported, so only the user's
            // collection is needed and Comic Vine is not called at all. A partially
            // imported edition does not qualify and falls through to resume the import.
            if (existingEdition.HasAllTomes(storedTomes.Count))
            {
                var collection = await GetOrAddUserCollectionAsync(
                    command.UserId,
                    existingEdition.Id,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new ImportComicVineVolumeResult(
                    EditionId: existingEdition.Id,
                    UserCollectionId: collection.Id,
                    ComicVineVolumeId: existingEdition.ComicVineVolumeId,
                    Title: existingEdition.Name,
                    PublisherName: existingEdition.PublisherName,
                    TotalIssues: storedTomes.Count,
                    ImportedTomes: storedTomes.Count,
                    IsCompleted: true);
            }
        }

        var volume = await _comicVineClient.GetVolumeByApiDetailUrlAsync(
            command.ApiDetailUrl,
            cancellationToken);

        if (volume is null)
        {
            throw new NotFoundException("Comic Vine volume was not found.");
        }

        if (volume.Issues.Count > MaxIssuesPerImport)
        {
            throw new ValidationException($"This volume has too many issues to import at once. Maximum allowed is {MaxIssuesPerImport}.");
        }

        var series = await _seriesRepository.GetByTitleAsync(
            volume.Name,
            cancellationToken);

        if (series is null)
        {
            series = new Series(
                title: volume.Name,
                originalTitle: null,
                description: volume.Description,
                imageUrl: volume.ImageUrl);

            await _seriesRepository.AddAsync(series, cancellationToken);
        }
        else
        {
            series.UpdateDetails(
                title: volume.Name,
                originalTitle: null,
                description: volume.Description,
                imageUrl: volume.ImageUrl);
        }

        var edition = existingEdition ?? await _editionRepository.GetByComicVineApiDetailUrlAsync(
            volume.ApiDetailUrl,
            cancellationToken);

        if (edition is null)
        {
            edition = new Edition(
                seriesId: series.Id,
                comicVineVolumeId: volume.ComicVineVolumeId,
                comicVineApiDetailUrl: volume.ApiDetailUrl,
                name: volume.Name,
                publisherName: volume.PublisherName,
                startYear: volume.StartYear,
                description: volume.Description,
                imageUrl: volume.ImageUrl,
                siteDetailUrl: volume.SiteDetailUrl,
                issueCount: volume.CountOfIssues);

            await _editionRepository.AddAsync(edition, cancellationToken);
        }
        else
        {
            edition.SyncDetails(
                name: volume.Name,
                publisherName: volume.PublisherName,
                startYear: volume.StartYear,
                description: volume.Description,
                imageUrl: volume.ImageUrl,
                siteDetailUrl: volume.SiteDetailUrl,
                issueCount: volume.CountOfIssues,
                syncedAt: _timeProvider.GetUtcNow());
        }

        var userCollection = await GetOrAddUserCollectionAsync(
            command.UserId,
            edition.Id,
            cancellationToken);

        var existingTomes = await _tomeRepository.GetByEditionIdAsync(
            edition.Id,
            cancellationToken);

        var storedIssueUrls = existingTomes
            .Select(tome => tome.ComicVineApiDetailUrl)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Only issues without a stored tome become new tomes, so running the same
        // import twice adds nothing and a partial import resumes where it stopped.
        var missingIssues = volume.Issues
            .Where(issue => !storedIssueUrls.Contains(issue.ApiDetailUrl))
            .OrderBy(issue => issue.NormalizedNumber ?? int.MaxValue)
            .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tomesWithData = volume.Issues.Count - missingIssues.Count;

        // One paged request per 100 issues for the whole volume, and none at all when
        // nothing is missing.
        var issueDetails = missingIssues.Count == 0
            ? new Dictionary<string, ComicVineIssueDetailDto>()
            : await GetIssueDetailsByUrlAsync(volume.ComicVineVolumeId, cancellationToken);

        foreach (var issueSummary in missingIssues)
        {
            if (!issueDetails.TryGetValue(issueSummary.ApiDetailUrl, out var issueDetail)
                || !storedIssueUrls.Add(issueDetail.ApiDetailUrl))
            {
                // Not listed by Comic Vine yet (retried on the next import) or a duplicate.
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

            tomesWithData++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImportComicVineVolumeResult(
            EditionId: edition.Id,
            UserCollectionId: userCollection.Id,
            ComicVineVolumeId: volume.ComicVineVolumeId,
            Title: volume.Name,
            PublisherName: volume.PublisherName,
            TotalIssues: volume.Issues.Count,
            ImportedTomes: tomesWithData,
            IsCompleted: tomesWithData == volume.Issues.Count);
    }

    private async Task<Dictionary<string, ComicVineIssueDetailDto>> GetIssueDetailsByUrlAsync(
        int comicVineVolumeId,
        CancellationToken cancellationToken)
    {
        var issues = await _comicVineClient.GetVolumeIssuesAsync(comicVineVolumeId, cancellationToken);

        return issues
            .DistinctBy(issue => issue.ApiDetailUrl, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(issue => issue.ApiDetailUrl, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<UserCollection> GetOrAddUserCollectionAsync(
        Guid userId,
        Guid editionId,
        CancellationToken cancellationToken)
    {
        var userCollection = await _userCollectionRepository.GetByUserIdAndEditionIdAsync(
            userId,
            editionId,
            cancellationToken);

        if (userCollection is not null)
        {
            return userCollection;
        }

        userCollection = new UserCollection(
            userId: userId,
            editionId: editionId);

        await _userCollectionRepository.AddAsync(userCollection, cancellationToken);

        return userCollection;
    }
}
