using AwesomeAssertions;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.EditionTomes;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.BackgroundJobs;
using MangaTracker.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace MangaTracker.Tests.Infrastructure.BackgroundJobs;

public sealed class TomeImportQueueTests
{
    [Fact]
    public async Task An_edition_should_be_queued_once_and_stay_pending_until_completed()
    {
        var queue = new TomeImportQueue();
        var editionId = Guid.NewGuid();

        await queue.EnqueueAsync(editionId);
        await queue.EnqueueAsync(editionId);

        queue.IsPending(editionId).Should().BeTrue();

        var queued = 0;
        while (queue.Reader.TryRead(out _))
        {
            queued++;
        }

        queued.Should().Be(1, "a second add of the same series while it downloads queues nothing");

        queue.Complete(editionId);
        queue.IsPending(editionId).Should().BeFalse();
    }

    [Fact]
    public async Task The_background_service_should_import_the_tomes_and_clear_the_pending_flag()
    {
        var edition = new Edition(Guid.NewGuid(), 21397, "https://comicvine.gamespot.com/api/volume/4050-21397/", "One Piece");
        var editionRepository = Substitute.For<IEditionRepository>();
        var tomeRepository = Substitute.For<ITomeRepository>();
        var comicVineClient = Substitute.For<IComicVineClient>();
        editionRepository.GetByIdWithTomesAsync(edition.Id, Arg.Any<CancellationToken>()).Returns(edition);
        tomeRepository
            .GetByComicVineApiDetailUrlsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        comicVineClient
            .GetVolumeIssuesAsync(21397, Arg.Any<CancellationToken>())
            .Returns([ComicVineIssues.Detail(1), ComicVineIssues.Detail(2)]);

        var provider = new ServiceCollection()
            .AddSingleton(editionRepository)
            .AddSingleton(tomeRepository)
            .AddSingleton(comicVineClient)
            .AddSingleton(Substitute.For<IUnitOfWork>())
            .AddScoped<EditionTomeImporter>()
            .BuildServiceProvider();

        var queue = new TomeImportQueue();
        using var service = new TomeImportBackgroundService(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<TomeImportBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await queue.EnqueueAsync(edition.Id);

        // Wait (bounded) for the worker to finish the edition.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (queue.IsPending(edition.Id) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        queue.IsPending(edition.Id).Should().BeFalse();
        await tomeRepository.Received(2).AddAsync(Arg.Any<Tome>(), Arg.Any<CancellationToken>());

        await service.StopAsync(CancellationToken.None);
    }
}
