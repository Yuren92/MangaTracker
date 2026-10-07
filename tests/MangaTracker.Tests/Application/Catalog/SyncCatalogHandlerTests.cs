using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.SyncCatalog;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Application.Common.Exceptions;
using MangaTracker.Domain.Entities;
using MangaTracker.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MangaTracker.Tests.Application.Catalog;

public sealed class SyncCatalogHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IEditionRepository _editionRepository = Substitute.For<IEditionRepository>();
    private readonly ITomeRepository _tomeRepository = Substitute.For<ITomeRepository>();
    private readonly IComicVineClient _comicVineClient = Substitute.For<IComicVineClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public SyncCatalogHandlerTests()
    {
        _tomeRepository
            .GetByComicVineApiDetailUrlsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public async Task Should_check_up_to_100_editions_per_request_and_load_nothing_when_none_changed()
    {
        var candidates = Enumerable.Range(1, 150)
            .Select(volumeId => new EditionSyncCandidate(Guid.NewGuid(), volumeId, StoredTomes: 10))
            .ToList();
        GivenCandidates(candidates);
        GivenIssueCounts(candidates.ToDictionary(candidate => candidate.ComicVineVolumeId, _ => 10));

        var result = await CreateHandler().HandleAsync();

        result.CheckedEditions.Should().Be(150);
        result.ChangedEditions.Should().Be(0);
        result.Completed.Should().BeTrue();

        await _comicVineClient.Received(2).GetVolumeIssueCountsAsync(
            Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
        await _comicVineClient.DidNotReceiveWithAnyArgs().GetVolumeByApiDetailUrlAsync(default!, default);
        await _editionRepository.DidNotReceiveWithAnyArgs().GetByIdWithTomesAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Should_fetch_and_add_tomes_only_for_editions_that_grew()
    {
        var unchanged = NewEdition(volumeId: 1, storedIssues: [1, 2]);
        var grown = NewEdition(volumeId: 2, storedIssues: [1, 2]);
        GivenCandidates([Candidate(unchanged), Candidate(grown)]);
        GivenIssueCounts(new Dictionary<int, int> { [1] = 2, [2] = 3 });
        GivenComicVineVolume(grown, issueNumbers: [1, 2, 3]);

        var result = await CreateHandler().HandleAsync();

        result.ChangedEditions.Should().Be(1);
        result.SyncedEditions.Should().Be(1);
        result.NewTomes.Should().Be(1);

        await _editionRepository.DidNotReceive().GetByIdWithTomesAsync(unchanged.Id, Arg.Any<CancellationToken>());
        await _tomeRepository.Received(1).AddAsync(
            Arg.Is<Tome>(tome => tome.ComicVineIssueId == ComicVineIssues.Id(3)), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        grown.LastSyncedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_skip_volumes_comic_vine_no_longer_lists()
    {
        var gone = NewEdition(volumeId: 9, storedIssues: [1]);
        GivenCandidates([Candidate(gone)]);
        GivenIssueCounts([]);

        var result = await CreateHandler().HandleAsync();

        result.ChangedEditions.Should().Be(0);
        await _editionRepository.DidNotReceiveWithAnyArgs().GetByIdWithTomesAsync(default, default);
    }

    [Fact]
    public async Task Should_stop_without_writing_when_comic_vine_is_down_while_checking()
    {
        GivenCandidates([new EditionSyncCandidate(Guid.NewGuid(), 1, 1)]);
        _comicVineClient
            .GetVolumeIssueCountsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalServiceUnavailableException("down", new HttpRequestException()));

        var result = await CreateHandler().HandleAsync();

        result.Completed.Should().BeFalse();
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Should_keep_what_was_saved_and_stop_when_comic_vine_goes_down_mid_run()
    {
        var first = NewEdition(volumeId: 1, storedIssues: [1]);
        var second = NewEdition(volumeId: 2, storedIssues: [1]);
        var third = NewEdition(volumeId: 3, storedIssues: [1]);
        GivenCandidates([Candidate(first), Candidate(second), Candidate(third)]);
        GivenIssueCounts(new Dictionary<int, int> { [1] = 2, [2] = 2, [3] = 2 });
        GivenComicVineVolume(first, issueNumbers: [1, 2]);
        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(second.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalServiceUnavailableException("down", new HttpRequestException()));

        var result = await CreateHandler().HandleAsync();

        result.Completed.Should().BeFalse();
        result.SyncedEditions.Should().Be(1);
        _unitOfWork.Received(1).DiscardChanges();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _editionRepository.DidNotReceive().GetByIdWithTomesAsync(third.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_skip_an_edition_that_fails_on_its_own_and_sync_the_rest()
    {
        var broken = NewEdition(volumeId: 1, storedIssues: [1]);
        var healthy = NewEdition(volumeId: 2, storedIssues: [1]);
        GivenCandidates([Candidate(broken), Candidate(healthy)]);
        GivenIssueCounts(new Dictionary<int, int> { [1] = 2, [2] = 2 });
        GivenComicVineVolume(healthy, issueNumbers: [1, 2]);
        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(broken.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("unexpected data"));

        var result = await CreateHandler().HandleAsync();

        result.Completed.Should().BeTrue();
        result.FailedEditions.Should().Be(1);
        result.SyncedEditions.Should().Be(1);
        result.NewTomes.Should().Be(1);
        _unitOfWork.Received(1).DiscardChanges();
    }

    [Fact]
    public async Task Should_not_swallow_cancellation()
    {
        var edition = NewEdition(volumeId: 1, storedIssues: [1]);
        GivenCandidates([Candidate(edition)]);
        GivenIssueCounts(new Dictionary<int, int> { [1] = 2 });
        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(edition.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => CreateHandler().HandleAsync();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private SyncCatalogHandler CreateHandler()
    {
        return new SyncCatalogHandler(
            _editionRepository,
            _tomeRepository,
            _comicVineClient,
            _unitOfWork,
            new FakeTimeProvider(Now),
            NullLogger<SyncCatalogHandler>.Instance);
    }

    private void GivenCandidates(IReadOnlyCollection<EditionSyncCandidate> candidates)
    {
        _editionRepository.GetSyncCandidatesAsync(Arg.Any<CancellationToken>()).Returns(candidates);
    }

    private void GivenIssueCounts(Dictionary<int, int> counts)
    {
        _comicVineClient
            .GetVolumeIssueCountsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var requested = call.Arg<IReadOnlyCollection<int>>();
                IReadOnlyDictionary<int, int> answer = counts
                    .Where(pair => requested.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                return answer;
            });
    }

    private Edition NewEdition(int volumeId, int[] storedIssues)
    {
        var edition = new Edition(
            seriesId: Guid.NewGuid(),
            comicVineVolumeId: volumeId,
            comicVineApiDetailUrl: $"https://comicvine.gamespot.com/api/volume/4050-{volumeId}/",
            name: $"Volume {volumeId}");

        foreach (var number in storedIssues)
        {
            PersistedEntities.AddTome(edition, new Tome(
                editionId: edition.Id,
                comicVineIssueId: ComicVineIssues.Id(number),
                comicVineApiDetailUrl: ComicVineIssues.Url(number),
                issueNumber: number.ToString(),
                normalizedNumber: number));
        }

        _editionRepository.GetByIdWithTomesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);

        return edition;
    }

    private static EditionSyncCandidate Candidate(Edition edition)
    {
        return new EditionSyncCandidate(edition.Id, edition.ComicVineVolumeId, edition.Tomes.Count);
    }

    private void GivenComicVineVolume(Edition edition, int[] issueNumbers)
    {
        var volume = new ComicVineVolumeDetailDto(
            ComicVineVolumeId: edition.ComicVineVolumeId,
            Name: edition.Name,
            PublisherName: null,
            CountOfIssues: issueNumbers.Length,
            ImageUrl: null,
            StartYear: null,
            Description: null,
            SiteDetailUrl: null,
            ApiDetailUrl: edition.ComicVineApiDetailUrl,
            Issues: issueNumbers.Select(ComicVineIssues.Summary).ToList());

        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(edition.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .Returns(volume);
        _comicVineClient
            .GetVolumeIssuesAsync(edition.ComicVineVolumeId, Arg.Any<CancellationToken>())
            .Returns(issueNumbers.Select(ComicVineIssues.Detail).ToList());
    }
}
