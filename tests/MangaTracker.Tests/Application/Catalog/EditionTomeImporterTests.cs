using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.EditionTomes;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using MangaTracker.Tests.Fakes;
using NSubstitute;

namespace MangaTracker.Tests.Application.Catalog;

public sealed class EditionTomeImporterTests
{
    private readonly IEditionRepository _editionRepository = Substitute.For<IEditionRepository>();
    private readonly ITomeRepository _tomeRepository = Substitute.For<ITomeRepository>();
    private readonly IComicVineClient _comicVineClient = Substitute.For<IComicVineClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public EditionTomeImporterTests()
    {
        _tomeRepository
            .GetByComicVineApiDetailUrlsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task Should_add_only_the_issues_without_a_tome_and_save_once()
    {
        var edition = GivenEdition(storedIssues: [1, 2]);
        _comicVineClient
            .GetVolumeIssuesAsync(edition.ComicVineVolumeId, Arg.Any<CancellationToken>())
            .Returns([ComicVineIssues.Detail(1), ComicVineIssues.Detail(2), ComicVineIssues.Detail(3), ComicVineIssues.Detail(4)]);

        var added = await CreateImporter().ImportMissingTomesAsync(edition.Id);

        added.Should().Be(2);
        await _tomeRepository.Received(2).AddAsync(Arg.Any<Tome>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_refresh_an_issue_already_stored_under_another_edition_instead_of_duplicating_it()
    {
        var edition = GivenEdition(storedIssues: []);
        var storedElsewhere = new Tome(Guid.NewGuid(), ComicVineIssues.Id(1), ComicVineIssues.Url(1), "1", 1, title: "Old title");
        _tomeRepository
            .GetByComicVineApiDetailUrlsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([storedElsewhere]);

        var added = await CreateImporter().AddMissingTomesAsync(edition, [ComicVineIssues.Detail(1)]);

        added.Should().Be(0);
        storedElsewhere.Title.Should().Be("Volume 1");
        await _tomeRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Should_retry_once_from_a_clean_state_when_a_concurrent_import_stored_the_same_tomes()
    {
        var edition = GivenEdition(storedIssues: []);
        _comicVineClient
            .GetVolumeIssuesAsync(edition.ComicVineVolumeId, Arg.Any<CancellationToken>())
            .Returns([ComicVineIssues.Detail(1)]);
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(
                _ => throw new UniqueConstraintViolationException("conflict", new Exception()),
                _ => Task.CompletedTask);

        await CreateImporter().ImportMissingTomesAsync(edition.Id);

        _unitOfWork.Received(1).DiscardChanges();
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_do_nothing_for_an_edition_removed_while_it_was_queued()
    {
        var added = await CreateImporter().ImportMissingTomesAsync(Guid.NewGuid());

        added.Should().Be(0);
        await _comicVineClient.DidNotReceiveWithAnyArgs().GetVolumeIssuesAsync(default, default);
    }

    private EditionTomeImporter CreateImporter()
    {
        return new EditionTomeImporter(_editionRepository, _tomeRepository, _comicVineClient, _unitOfWork);
    }

    private Edition GivenEdition(int[] storedIssues)
    {
        var edition = new Edition(
            seriesId: Guid.NewGuid(),
            comicVineVolumeId: 21397,
            comicVineApiDetailUrl: "https://comicvine.gamespot.com/api/volume/4050-21397/",
            name: "One Piece");

        foreach (var number in storedIssues)
        {
            PersistedEntities.AddTome(edition, new Tome(
                edition.Id, ComicVineIssues.Id(number), ComicVineIssues.Url(number), number.ToString(), number));
        }

        _editionRepository.GetByIdWithTomesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);

        return edition;
    }
}
