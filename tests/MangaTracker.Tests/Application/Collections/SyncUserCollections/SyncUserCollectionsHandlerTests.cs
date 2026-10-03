using FluentAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Collections.SyncUserCollections;
using MangaTracker.Application.ComicVine.Dtos;
using MangaTracker.Domain.Entities;
using MangaTracker.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MangaTracker.Tests.Application.Collections.SyncUserCollections;

public sealed class SyncUserCollectionsHandlerTests
{
    // Editions created in a test get ImportedAt = real "now", so the fake clock runs
    // well ahead of it: a never-synced edition is always due.
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow.AddDays(30);

    private readonly IUserCollectionRepository _userCollectionRepository = Substitute.For<IUserCollectionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITomeRepository _tomeRepository = Substitute.For<ITomeRepository>();
    private readonly IComicVineClient _comicVineClient = Substitute.For<IComicVineClient>();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task HandleAsync_ShouldSkipCollectionsInsideTheCooldown()
    {
        var edition = NewEdition(lastSyncedHoursAgo: 2);
        GivenUserCollections(edition);

        var result = await CreateHandler().HandleAsync(Command());

        result.SkippedCollections.Should().Be(1);
        result.SyncedCollections.Should().Be(0);
        await _comicVineClient.DidNotReceiveWithAnyArgs().GetVolumeByApiDetailUrlAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ShouldSyncAtMostFiveCollectionsAndDeferTheRest()
    {
        var editions = Enumerable.Range(0, 8).Select(_ => NewEdition(lastSyncedHoursAgo: 48)).ToArray();
        GivenUserCollections(editions);
        GivenComicVineVolumesFor(editions);

        var result = await CreateHandler().HandleAsync(Command());

        result.CheckedCollections.Should().Be(8);
        result.SyncedCollections.Should().Be(5);
        result.DeferredCollections.Should().Be(3);
        result.SkippedCollections.Should().Be(0);
        await _comicVineClient.ReceivedWithAnyArgs(5).GetVolumeByApiDetailUrlAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_ShouldReachCollectionsBeyondTheFirstFive_WhenTheFirstFiveAreInCooldown()
    {
        // Regression: the limit was applied before the cooldown filter, so collections
        // 6..10 were never even considered while 1..5 were fresh.
        var fresh = Enumerable.Range(0, 5).Select(_ => NewEdition(lastSyncedHoursAgo: 1)).ToArray();
        var stale = Enumerable.Range(0, 5).Select(_ => NewEdition(lastSyncedHoursAgo: 72)).ToArray();
        GivenUserCollections([.. fresh, .. stale]);
        GivenComicVineVolumesFor(stale);

        var result = await CreateHandler().HandleAsync(Command());

        result.SyncedCollections.Should().Be(5);
        result.SkippedCollections.Should().Be(5);

        foreach (var edition in stale)
        {
            edition.LastSyncedAt.Should().Be(Now);
        }
    }

    [Fact]
    public async Task HandleAsync_ShouldSyncTheLeastRecentlySyncedCollectionsFirst()
    {
        var newest = NewEdition(lastSyncedHoursAgo: 30);
        var oldest = Enumerable.Range(0, 5).Select(hours => NewEdition(lastSyncedHoursAgo: 100 + hours)).ToArray();
        GivenUserCollections([newest, .. oldest]);
        GivenComicVineVolumesFor([newest, .. oldest]);

        var result = await CreateHandler().HandleAsync(Command());

        result.SyncedCollections.Should().Be(5);
        result.DeferredCollections.Should().Be(1);
        await _comicVineClient.DidNotReceive().GetVolumeByApiDetailUrlAsync(newest.ComicVineApiDetailUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldAddTomesForNewIssues()
    {
        var edition = NewEdition(lastSyncedHoursAgo: 48);
        GivenUserCollections(edition);
        GivenComicVineVolume(edition, issueNumbers: [1, 2]);

        _comicVineClient
            .GetIssueByApiDetailUrlAsync(ComicVineIssues.Url(1), Arg.Any<CancellationToken>())
            .Returns(ComicVineIssues.Detail(1));
        _comicVineClient
            .GetIssueByApiDetailUrlAsync(ComicVineIssues.Url(2), Arg.Any<CancellationToken>())
            .Returns(ComicVineIssues.Detail(2));

        var result = await CreateHandler().HandleAsync(Command());

        result.NewTomes.Should().Be(2);
        await _tomeRepository.Received(2).AddAsync(Arg.Any<Tome>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkTheAttempt_WhenComicVineNoLongerHasTheVolume()
    {
        var missing = NewEdition(lastSyncedHoursAgo: 100);
        GivenUserCollections(missing);

        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(missing.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .Returns((ComicVineVolumeDetailDto?)null);

        var result = await CreateHandler().HandleAsync(Command());

        result.FailedCollections.Should().Be(1);
        result.SyncedCollections.Should().Be(0);

        // Without this, the edition would stay first in the queue on every run.
        missing.LastSyncedAt.Should().Be(Now);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepSyncingOtherCollections_WhenOneFails()
    {
        var broken = NewEdition(lastSyncedHoursAgo: 100);
        var healthy = NewEdition(lastSyncedHoursAgo: 50);
        GivenUserCollections(broken, healthy);
        GivenComicVineVolumesFor(healthy);

        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(broken.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("404 Not Found"));

        var result = await CreateHandler().HandleAsync(Command());

        result.FailedCollections.Should().Be(1);
        result.SyncedCollections.Should().Be(1);
        broken.LastSyncedAt.Should().Be(Now);
        healthy.LastSyncedAt.Should().Be(Now);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSwallowCancellation()
    {
        var edition = NewEdition(lastSyncedHoursAgo: 48);
        GivenUserCollections(edition);

        _comicVineClient
            .GetVolumeByApiDetailUrlAsync(edition.ComicVineApiDetailUrl, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = () => CreateHandler().HandleAsync(Command());

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private SyncUserCollectionsHandler CreateHandler()
    {
        return new SyncUserCollectionsHandler(
            _userCollectionRepository,
            _tomeRepository,
            _comicVineClient,
            new FakeTimeProvider(Now),
            NullLogger<SyncUserCollectionsHandler>.Instance,
            _unitOfWork);
    }

    private SyncUserCollectionsCommand Command() => new(UserId: _userId);

    private static Edition NewEdition(int lastSyncedHoursAgo)
    {
        var volumeId = Random.Shared.Next(1, 1_000_000);

        var edition = new Edition(
            seriesId: Guid.NewGuid(),
            comicVineVolumeId: volumeId,
            comicVineApiDetailUrl: $"https://comicvine.gamespot.com/api/volume/4050-{volumeId}/",
            name: $"Volume {volumeId}");

        edition.MarkSyncAttempted(Now.AddHours(-lastSyncedHoursAgo));

        return edition;
    }

    private void GivenUserCollections(params Edition[] editions)
    {
        var collections = editions
            .Select(edition => PersistedEntities.CollectionFor(edition, _userId))
            .ToList();

        _userCollectionRepository
            .GetAllByUserIdAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(collections);
    }

    private void GivenComicVineVolumesFor(params Edition[] editions)
    {
        foreach (var edition in editions)
        {
            GivenComicVineVolume(edition, issueNumbers: []);
        }
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
    }
}
