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

    public ImportComicVineVolumeHandler(
        IComicVineClient comicVineClient,
        ISeriesRepository seriesRepository,
        IEditionRepository editionRepository,
        ITomeRepository tomeRepository,
        IUserCollectionRepository userCollectionRepository)
    {
        _comicVineClient = comicVineClient;
        _seriesRepository = seriesRepository;
        _editionRepository = editionRepository;
        _tomeRepository = tomeRepository;
        _userCollectionRepository = userCollectionRepository;
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

        var existingEdition = await _editionRepository.GetByComicVineApiDetailUrlWithTomesAsync(
            command.ApiDetailUrl,
            cancellationToken);

        if (existingEdition is not null && existingEdition.Tomes.Count > 0)
        {
            var existingUserCollection = await _userCollectionRepository.GetByUserIdAndEditionIdAsync(
                command.UserId,
                existingEdition.Id,
                cancellationToken);

            if (existingUserCollection is null)
            {
                existingUserCollection = new UserCollection(
                    userId: command.UserId,
                    editionId: existingEdition.Id);

                await _userCollectionRepository.AddAsync(existingUserCollection, cancellationToken);
                await _userCollectionRepository.SaveChangesAsync(cancellationToken);
            }

            return new ImportComicVineVolumeResult(
                EditionId: existingEdition.Id,
                UserCollectionId: existingUserCollection.Id,
                ComicVineVolumeId: existingEdition.ComicVineVolumeId,
                Title: existingEdition.Series.Title,
                PublisherName: existingEdition.PublisherName,
                TotalIssues: existingEdition.Tomes.Count,
                ImportedTomes: existingEdition.Tomes.Count,
                IsCompleted: true);
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

        var edition = await _editionRepository.GetByComicVineApiDetailUrlAsync(
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
                issueCount: volume.CountOfIssues);
        }

        var userCollection = await _userCollectionRepository.GetByUserIdAndEditionIdAsync(
            command.UserId,
            edition.Id,
            cancellationToken);

        if (userCollection is null)
        {
            userCollection = new UserCollection(
                userId: command.UserId,
                editionId: edition.Id);

            await _userCollectionRepository.AddAsync(userCollection, cancellationToken);
        }

        var importedTomes = 0;

        var existingTomes = await _tomeRepository.GetByEditionIdAsync(
            edition.Id,
            cancellationToken);

        var existingTomesByApiDetailUrl = existingTomes
            .Where(tome => !string.IsNullOrWhiteSpace(tome.ComicVineApiDetailUrl))
            .ToDictionary(
                tome => tome.ComicVineApiDetailUrl,
                StringComparer.OrdinalIgnoreCase);

        var issueSummaries = volume.Issues
            .OrderBy(issue => issue.NormalizedNumber ?? int.MaxValue)
            .ThenBy(issue => issue.IssueNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var issueSummary in issueSummaries)
        {
            var issueDetail = await _comicVineClient.GetIssueByApiDetailUrlAsync(
                issueSummary.ApiDetailUrl,
                cancellationToken);

            if (issueDetail is null)
            {
                continue;
            }

            existingTomesByApiDetailUrl.TryGetValue(
                issueDetail.ApiDetailUrl,
                out var existingTome);

            if (existingTome is null)
            {
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
                existingTomesByApiDetailUrl[tome.ComicVineApiDetailUrl] = tome;
            }
            else
            {
                existingTome.SyncDetails(
                    issueNumber: issueDetail.IssueNumber,
                    normalizedNumber: issueDetail.NormalizedNumber,
                    title: issueDetail.Title,
                    imageUrl: issueDetail.ImageUrl,
                    coverDate: issueDetail.CoverDate,
                    storeDate: issueDetail.StoreDate,
                    siteDetailUrl: issueDetail.SiteDetailUrl);
            }

            importedTomes++;
        }

        await _userCollectionRepository.SaveChangesAsync(cancellationToken);

        return new ImportComicVineVolumeResult(
            EditionId: edition.Id,
            UserCollectionId: userCollection.Id,
            ComicVineVolumeId: volume.ComicVineVolumeId,
            Title: volume.Name,
            PublisherName: volume.PublisherName,
            TotalIssues: volume.Issues.Count,
            ImportedTomes: importedTomes,
            IsCompleted: importedTomes == volume.Issues.Count);
    }
}