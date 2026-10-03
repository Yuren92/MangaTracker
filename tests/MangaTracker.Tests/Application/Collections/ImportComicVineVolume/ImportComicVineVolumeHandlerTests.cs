using FluentAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Collections.ImportComicVineVolume;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using MangaTracker.Tests.Fakes;
using NSubstitute;

namespace MangaTracker.Tests.Application.Collections.ImportComicVineVolume;

public sealed class ImportComicVineVolumeHandlerTests
{
    private const string ApiDetailUrl = "https://comicvine.gamespot.com/api/volume/4050-21397/";

    private readonly IComicVineClient _comicVineClient = Substitute.For<IComicVineClient>();
    private readonly ISeriesRepository _seriesRepository = Substitute.For<ISeriesRepository>();
    private readonly IEditionRepository _editionRepository = Substitute.For<IEditionRepository>();
    private readonly ITomeRepository _tomeRepository = Substitute.For<ITomeRepository>();
    private readonly IUserCollectionRepository _userCollectionRepository = Substitute.For<IUserCollectionRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    public ImportComicVineVolumeHandlerTests()
    {
        _tomeRepository
            .GetByEditionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Tome>());
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenUserIdIsEmpty()
    {
        var command = new ImportComicVineVolumeCommand(UserId: Guid.Empty, ApiDetailUrl: ApiDetailUrl);

        var act = async () => await CreateHandler().HandleAsync(command);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("User id is required.");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenApiDetailUrlIsEmpty()
    {
        var command = new ImportComicVineVolumeCommand(UserId: Guid.NewGuid(), ApiDetailUrl: " ");

        var act = async () => await CreateHandler().HandleAsync(command);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("Comic Vine volume API detail URL is required.");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowNotFoundException_WhenComicVineVolumeDoesNotExist()
    {
        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(ApiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((ComicVineVolumeDetailDto?)null);

        var act = async () => await CreateHandler().HandleAsync(Command());

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Comic Vine volume was not found.");
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowValidationException_WhenVolumeHasTooManyIssues()
    {
        GivenComicVineVolume(issueCount: 251);

        var act = async () => await CreateHandler().HandleAsync(Command());

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("This volume has too many issues to import at once. Maximum allowed is 250.");
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateSeriesEditionCollectionAndAllTomes_WhenVolumeIsNew()
    {
        GivenComicVineVolume(issueCount: 3);
        GivenComicVineIssues(1, 2, 3);

        var result = await CreateHandler().HandleAsync(Command());

        result.ComicVineVolumeId.Should().Be(21397);
        result.Title.Should().Be("One Piece");
        result.PublisherName.Should().Be("Shueisha");
        result.TotalIssues.Should().Be(3);
        result.ImportedTomes.Should().Be(3);
        result.IsCompleted.Should().BeTrue();

        await _seriesRepository.Received(1).AddAsync(Arg.Any<Series>(), Arg.Any<CancellationToken>());
        await _editionRepository.Received(1).AddAsync(Arg.Any<Edition>(), Arg.Any<CancellationToken>());
        await _userCollectionRepository.Received(1).AddAsync(Arg.Any<UserCollection>(), Arg.Any<CancellationToken>());
        await _tomeRepository.Received(3).AddAsync(Arg.Any<Tome>(), Arg.Any<CancellationToken>());
        await _userCollectionRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallComicVine_WhenEditionIsAlreadyFullyImported()
    {
        var edition = GivenExistingEdition(issueCount: 3, storedIssueNumbers: [1, 2, 3]);

        var result = await CreateHandler().HandleAsync(Command());

        result.EditionId.Should().Be(edition.Id);
        result.IsCompleted.Should().BeTrue();
        result.ImportedTomes.Should().Be(3);

        await _comicVineClient.DidNotReceiveWithAnyArgs().GetVolumeByApiDetailUrlAsync(default!, default);
        await _userCollectionRepository.Received(1).AddAsync(Arg.Any<UserCollection>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReuseTheUserCollection_WhenTheUserAlreadyHasIt()
    {
        var edition = GivenExistingEdition(issueCount: 3, storedIssueNumbers: [1, 2, 3]);
        var existingCollection = new UserCollection(userId: _userId, editionId: edition.Id);

        _userCollectionRepository
            .GetByUserIdAndEditionIdAsync(_userId, edition.Id, Arg.Any<CancellationToken>())
            .Returns(existingCollection);

        var result = await CreateHandler().HandleAsync(Command());

        result.UserCollectionId.Should().Be(existingCollection.Id);
        await _userCollectionRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ShouldResumeAPartialImport_RequestingOnlyTheMissingIssues()
    {
        // A previous import stored tomes 1 and 2 of a 4 issue volume.
        GivenExistingEdition(issueCount: 4, storedIssueNumbers: [1, 2]);
        GivenComicVineVolume(issueCount: 4);
        GivenComicVineIssues(1, 2, 3, 4);

        var result = await CreateHandler().HandleAsync(Command());

        result.IsCompleted.Should().BeTrue();
        result.TotalIssues.Should().Be(4);
        result.ImportedTomes.Should().Be(4);

        await _comicVineClient.Received(1).GetIssueByApiDetailUrlAsync(IssueUrl(3), Arg.Any<CancellationToken>());
        await _comicVineClient.Received(1).GetIssueByApiDetailUrlAsync(IssueUrl(4), Arg.Any<CancellationToken>());
        await _comicVineClient.DidNotReceive().GetIssueByApiDetailUrlAsync(IssueUrl(1), Arg.Any<CancellationToken>());
        await _comicVineClient.DidNotReceive().GetIssueByApiDetailUrlAsync(IssueUrl(2), Arg.Any<CancellationToken>());

        await _tomeRepository.Received(2).AddAsync(Arg.Any<Tome>(), Arg.Any<CancellationToken>());
        await _editionRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportIncompleteImport_WhenAnIssueDetailIsMissing()
    {
        GivenComicVineVolume(issueCount: 3);
        GivenComicVineIssues(1, 3);

        _comicVineClient
            .GetIssueByApiDetailUrlAsync(IssueUrl(2), Arg.Any<CancellationToken>())
            .Returns((ComicVineIssueDetailDto?)null);

        var result = await CreateHandler().HandleAsync(Command());

        result.IsCompleted.Should().BeFalse();
        result.TotalIssues.Should().Be(3);
        result.ImportedTomes.Should().Be(2);

        await _tomeRepository.Received(2).AddAsync(Arg.Any<Tome>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotTreatAPartialImportAsComplete()
    {
        // Regression: any stored tome used to make the edition count as fully imported.
        GivenExistingEdition(issueCount: 100, storedIssueNumbers: [1]);
        GivenComicVineVolume(issueCount: 2);
        GivenComicVineIssues(2);

        _comicVineClient
            .GetIssueByApiDetailUrlAsync(IssueUrl(2), Arg.Any<CancellationToken>())
            .Returns((ComicVineIssueDetailDto?)null);

        var result = await CreateHandler().HandleAsync(Command());

        result.IsCompleted.Should().BeFalse();
        await _comicVineClient.Received(1).GetVolumeByApiDetailUrlAsync(ApiDetailUrl, Arg.Any<CancellationToken>());
    }

    private ImportComicVineVolumeHandler CreateHandler()
    {
        return new ImportComicVineVolumeHandler(
            _comicVineClient,
            _seriesRepository,
            _editionRepository,
            _tomeRepository,
            _userCollectionRepository,
            new FakeTimeProvider(DateTimeOffset.UtcNow));
    }

    private ImportComicVineVolumeCommand Command()
    {
        return new ImportComicVineVolumeCommand(UserId: _userId, ApiDetailUrl: ApiDetailUrl);
    }

    private Edition GivenExistingEdition(int issueCount, int[] storedIssueNumbers)
    {
        var edition = new Edition(
            seriesId: Guid.NewGuid(),
            comicVineVolumeId: 21397,
            comicVineApiDetailUrl: ApiDetailUrl,
            name: "One Piece",
            publisherName: "Shueisha",
            issueCount: issueCount);

        _editionRepository
            .GetByComicVineApiDetailUrlAsync(ApiDetailUrl, Arg.Any<CancellationToken>())
            .Returns(edition);

        var storedTomes = storedIssueNumbers
            .Select(number => new Tome(
                editionId: edition.Id,
                comicVineIssueId: IssueId(number),
                comicVineApiDetailUrl: IssueUrl(number),
                issueNumber: number.ToString(),
                normalizedNumber: number))
            .ToList();

        _tomeRepository
            .GetByEditionIdAsync(edition.Id, Arg.Any<CancellationToken>())
            .Returns(storedTomes);

        return edition;
    }

    private void GivenComicVineVolume(int issueCount)
    {
        var issues = Enumerable
            .Range(1, issueCount)
            .Select(number => new ComicVineIssueSummaryDto(
                ComicVineIssueId: IssueId(number),
                IssueNumber: number.ToString(),
                NormalizedNumber: number,
                Title: $"Volume {number}",
                SiteDetailUrl: $"https://comicvine.gamespot.com/one-piece-{number}/4000-{IssueId(number)}/",
                ApiDetailUrl: IssueUrl(number)))
            .ToList();

        var volume = new ComicVineVolumeDetailDto(
            ComicVineVolumeId: 21397,
            Name: "One Piece",
            PublisherName: "Shueisha",
            CountOfIssues: issues.Count,
            ImageUrl: "https://comicvine.gamespot.com/one-piece.jpg",
            StartYear: 1997,
            Description: "Japanese manga series.",
            SiteDetailUrl: "https://comicvine.gamespot.com/one-piece/4050-21397/",
            ApiDetailUrl: ApiDetailUrl,
            Issues: issues);

        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(ApiDetailUrl, Arg.Any<CancellationToken>())
            .Returns(volume);
    }

    private void GivenComicVineIssues(params int[] numbers)
    {
        foreach (var number in numbers)
        {
            _comicVineClient
                .GetIssueByApiDetailUrlAsync(IssueUrl(number), Arg.Any<CancellationToken>())
                .Returns(ComicVineIssues.Detail(number));
        }
    }

    private static int IssueId(int number) => ComicVineIssues.Id(number);

    private static string IssueUrl(int number) => ComicVineIssues.Url(number);
}
