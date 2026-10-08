using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.EditionTomes;
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
    private readonly Dictionary<int, ComicVineVolumeSummaryDto> _comicVineVolumes = [];

    public SyncCatalogHandlerTests()
    {
        _tomeRepository
            .GetByComicVineApiDetailUrlsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _comicVineClient
            .GetVolumeSummariesAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<IReadOnlyCollection<int>>()
                .Where(_comicVineVolumes.ContainsKey)
                .Select(id => _comicVineVolumes[id])
                .ToList());

        GivenIssuesAddedSince();
    }

    [Fact]
    public async Task Should_check_up_to_100_editions_per_request_and_load_nothing_when_none_changed()
    {
        var candidates = Enumerable.Range(1, 150)
            .Select(volumeId => new EditionSyncCandidate(Guid.NewGuid(), volumeId, StoredTomes: 10, Now.AddDays(-3)))
            .ToList();
        GivenCandidates(candidates);
        foreach (var candidate in candidates)
        {
            GivenComicVineVolume(candidate.ComicVineVolumeId, issueCount: 10);
        }

        var result = await CreateHandler().HandleAsync();

        result.CheckedEditions.Should().Be(150);
        result.ChangedEditions.Should().Be(0);
        result.Completed.Should().BeTrue();

        await _comicVineClient.Received(2).GetVolumeSummariesAsync(
            Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
        await _comicVineClient.DidNotReceiveWithAnyArgs().GetIssuesAddedSinceAsync(default!, default, default);
        await _editionRepository.DidNotReceiveWithAnyArgs().GetByIdWithTomesAsync(default, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Should_add_new_issues_from_one_shared_request_and_refresh_the_edition()
    {
        var unchanged = NewEdition(volumeId: 1, storedIssues: [1, 2]);
        var grown = NewEdition(volumeId: 2, storedIssues: [1, 2]);
        GivenCandidates([Candidate(unchanged), Candidate(grown)]);
        GivenComicVineVolume(1, issueCount: 2);
        GivenComicVineVolume(2, issueCount: 3, name: "Renamed volume");
        GivenIssuesAddedSince(Issue(volumeId: 2, number: 3));

        var result = await CreateHandler().HandleAsync();

        result.ChangedEditions.Should().Be(1);
        result.SyncedEditions.Should().Be(1);
        result.NewTomes.Should().Be(1);

        await _tomeRepository.Received(1).AddAsync(
            Arg.Is<Tome>(tome => tome.ComicVineIssueId == ComicVineIssues.Id(3)), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _editionRepository.DidNotReceive().GetByIdWithTomesAsync(unchanged.Id, Arg.Any<CancellationToken>());

        // No per-edition volume request and no full issue list: the batched data was enough.
        await _comicVineClient.DidNotReceiveWithAnyArgs().GetVolumeByApiDetailUrlAsync(default!, default);
        await _comicVineClient.DidNotReceiveWithAnyArgs().GetVolumeIssuesAsync(default, default);

        grown.Name.Should().Be("Renamed volume");
        grown.LastSyncedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_ask_for_the_new_issues_of_every_changed_edition_at_once()
    {
        var first = NewEdition(volumeId: 1, storedIssues: [1], lastRefreshedAt: Now.AddDays(-2));
        var second = NewEdition(volumeId: 2, storedIssues: [1], lastRefreshedAt: Now.AddDays(-10));
        GivenCandidates([Candidate(first), Candidate(second)]);
        GivenComicVineVolume(1, issueCount: 2);
        GivenComicVineVolume(2, issueCount: 2);
        GivenIssuesAddedSince(Issue(1, 2), Issue(2, 2));

        var result = await CreateHandler().HandleAsync();

        result.NewTomes.Should().Be(2);

        // Since the oldest of their last syncs, with a day of margin.
        await _comicVineClient.Received(1).GetIssuesAddedSinceAsync(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Order().SequenceEqual(new[] { 1, 2 })),
            Now.AddDays(-11),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_download_the_full_list_for_an_edition_still_short_after_the_date_filter()
    {
        // A partial import: issues 2 and 3 are old, so the date filter does not return them.
        var partial = NewEdition(volumeId: 1, storedIssues: [1]);
        GivenCandidates([Candidate(partial)]);
        GivenComicVineVolume(1, issueCount: 3);
        _comicVineClient
            .GetVolumeIssuesAsync(1, Arg.Any<CancellationToken>())
            .Returns([ComicVineIssues.Detail(1), ComicVineIssues.Detail(2), ComicVineIssues.Detail(3)]);

        var result = await CreateHandler().HandleAsync();

        result.NewTomes.Should().Be(2);
        await _comicVineClient.Received(1).GetVolumeIssuesAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_skip_volumes_comic_vine_no_longer_lists()
    {
        var gone = NewEdition(volumeId: 9, storedIssues: [1]);
        GivenCandidates([Candidate(gone)]);

        var result = await CreateHandler().HandleAsync();

        result.ChangedEditions.Should().Be(0);
        await _editionRepository.DidNotReceiveWithAnyArgs().GetByIdWithTomesAsync(default, default);
    }

    [Fact]
    public async Task Should_stop_without_writing_when_comic_vine_is_down_while_checking()
    {
        GivenCandidates([new EditionSyncCandidate(Guid.NewGuid(), 1, 1, Now)]);
        _comicVineClient
            .GetVolumeSummariesAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
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
        GivenComicVineVolume(1, issueCount: 2);
        GivenComicVineVolume(2, issueCount: 2);
        GivenComicVineVolume(3, issueCount: 2);
        GivenIssuesAddedSince(Issue(1, 2));

        // The second edition needs its full list (still short) and Comic Vine fails there.
        _comicVineClient
            .GetVolumeIssuesAsync(2, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalServiceUnavailableException("down", new HttpRequestException()));

        var result = await CreateHandler().HandleAsync();

        result.Completed.Should().BeFalse();
        result.SyncedEditions.Should().Be(1);
        _unitOfWork.Received(1).DiscardChanges();
        await _editionRepository.DidNotReceive().GetByIdWithTomesAsync(third.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_skip_an_edition_that_fails_on_its_own_and_sync_the_rest()
    {
        var broken = NewEdition(volumeId: 1, storedIssues: [1]);
        var healthy = NewEdition(volumeId: 2, storedIssues: [1]);
        GivenCandidates([Candidate(broken), Candidate(healthy)]);
        GivenComicVineVolume(1, issueCount: 2);
        GivenComicVineVolume(2, issueCount: 2);
        GivenIssuesAddedSince(Issue(1, 2), Issue(2, 2));
        _editionRepository
            .GetByIdWithTomesAsync(broken.Id, Arg.Any<CancellationToken>())
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
        GivenComicVineVolume(1, issueCount: 2);
        _editionRepository
            .GetByIdWithTomesAsync(edition.Id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => CreateHandler().HandleAsync();

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private SyncCatalogHandler CreateHandler()
    {
        return new SyncCatalogHandler(
            _editionRepository,
            _comicVineClient,
            new EditionTomeImporter(_editionRepository, _tomeRepository, _comicVineClient, _unitOfWork),
            _unitOfWork,
            new FakeTimeProvider(Now),
            NullLogger<SyncCatalogHandler>.Instance);
    }

    private void GivenCandidates(IReadOnlyCollection<EditionSyncCandidate> candidates)
    {
        _editionRepository.GetSyncCandidatesAsync(Arg.Any<CancellationToken>()).Returns(candidates);
    }

    private void GivenComicVineVolume(int volumeId, int issueCount, string? name = null)
    {
        _comicVineVolumes[volumeId] = new ComicVineVolumeSummaryDto(
            volumeId, name ?? $"Volume {volumeId}", null, issueCount, null, null, null);
    }

    private void GivenIssuesAddedSince(params ComicVineIssueDetailDto[] issues)
    {
        _comicVineClient
            .GetIssuesAddedSinceAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(issues);
    }

    private static ComicVineIssueDetailDto Issue(int volumeId, int number)
    {
        return ComicVineIssues.Detail(number) with { ComicVineVolumeId = volumeId };
    }

    private Edition NewEdition(int volumeId, int[] storedIssues, DateTimeOffset? lastRefreshedAt = null)
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
        _lastRefreshed[edition.Id] = lastRefreshedAt ?? Now.AddDays(-2);

        return edition;
    }

    private readonly Dictionary<Guid, DateTimeOffset> _lastRefreshed = [];

    private EditionSyncCandidate Candidate(Edition edition)
    {
        return new EditionSyncCandidate(
            edition.Id, edition.ComicVineVolumeId, edition.Tomes.Count, _lastRefreshed[edition.Id]);
    }
}
