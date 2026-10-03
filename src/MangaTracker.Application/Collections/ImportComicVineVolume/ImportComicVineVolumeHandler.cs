using MangaTracker.Application.Abstractions;
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

    public ImportComicVineVolumeHandler(
        IComicVineClient comicVineClient,
        ISeriesRepository seriesRepository,
        IEditionRepository editionRepository,
        ITomeRepository tomeRepository,
        IUserCollectionRepository userCollectionRepository,
        TimeProvider timeProvider)
    {
        _comicVineClient = comicVineClient;
        _seriesRepository = seriesRepository;
        _editionRepository = editionRepository;
        _tomeRepository = tomeRepository;
        _userCollectionRepository = userCollectionRepository;
        _timeProvider = timeProvider;
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

                await _userCollectionRepository.SaveChangesAsync(cancellationToken);

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

        // Only issues without a stored tome are requested, so retrying a partial
        // import costs one call per missing issue instead of one per issue, and
        // running the same import twice adds nothing.
        var missingIssues = volume.Issues
            .Where(issue => !storedIssueUrls.Contains(issue.ApiDetailUrl))
            .OrderBy(issue => issue.NormalizedNumber ?? int.MaxValue)
            .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tomesWithData = volume.Issues.Count - missingIssues.Count;

        foreach (var issueSummary in missingIssues)
        {
            var issueDetail = await _comicVineClient.GetIssueByApiDetailUrlAsync(
                issueSummary.ApiDetailUrl,
                cancellationToken);

            if (issueDetail is null || !storedIssueUrls.Add(issueDetail.ApiDetailUrl))
            {
                // Missing in Comic Vine (retried on the next import) or a duplicate.
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

        await _userCollectionRepository.SaveChangesAsync(cancellationToken);

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
