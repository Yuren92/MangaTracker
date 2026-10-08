using System.Collections.Concurrent;
using System.Threading.Channels;
using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Catalog.EditionTomes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MangaTracker.Infrastructure.BackgroundJobs;

// In-memory queue of editions whose tomes are still to be downloaded. Each edition is
// queued at most once at a time, and IsPending tells the shelf to show "downloading".
// Lost on restart by design: the daily catalog sync completes any partial edition.
public sealed class TomeImportQueue : ITomeImportQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    private readonly ConcurrentDictionary<Guid, byte> _pending = new();

    public ChannelReader<Guid> Reader => _channel.Reader;

    public async Task EnqueueAsync(Guid editionId, CancellationToken cancellationToken = default)
    {
        if (_pending.TryAdd(editionId, 0))
        {
            await _channel.Writer.WriteAsync(editionId, cancellationToken);
        }
    }

    public bool IsPending(Guid editionId) => _pending.ContainsKey(editionId);

    public void Complete(Guid editionId) => _pending.TryRemove(editionId, out _);
}

// Downloads the tomes of queued editions one edition at a time; the Comic Vine request
// gate already spaces the requests, so working in parallel would gain nothing.
public sealed class TomeImportBackgroundService : BackgroundService
{
    private readonly TomeImportQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TomeImportBackgroundService> _logger;

    public TomeImportBackgroundService(
        TomeImportQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TomeImportBackgroundService> logger)
    {
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var editionId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                var added = await scope.ServiceProvider
                    .GetRequiredService<EditionTomeImporter>()
                    .ImportMissingTomesAsync(editionId, stoppingToken);

                _logger.LogInformation("Imported {Tomes} tomes for edition {EditionId}", added, editionId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // The edition stays partial; the daily catalog sync will complete it.
                _logger.LogWarning(exception, "Could not import the tomes of edition {EditionId}", editionId);
            }
            finally
            {
                _queue.Complete(editionId);
            }
        }
    }
}
