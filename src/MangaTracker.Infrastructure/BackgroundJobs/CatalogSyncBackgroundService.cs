using MangaTracker.Application.Catalog.SyncCatalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangaTracker.Infrastructure.BackgroundJobs;

// Runs the catalog sync shortly after the API starts and then every IntervalHours.
// The host may recycle the process when idle, so a run after each start is expected;
// it is cheap (one Comic Vine request per 100 editions when nothing changed).
public sealed class CatalogSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CatalogSyncBackgroundService> _logger;
    private readonly CatalogSyncOptions _options;

    public CatalogSyncBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CatalogSyncBackgroundService> logger,
        IOptions<CatalogSyncOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        // Let startup and the first requests go first.
        await Task.Delay(TimeSpan.FromMinutes(_options.InitialDelayMinutes), stoppingToken);

        var interval = TimeSpan.FromHours(_options.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // A fresh scope per run: the handler uses scoped services (DbContext, repositories).
                using var scope = _serviceScopeFactory.CreateScope();

                await scope.ServiceProvider
                    .GetRequiredService<SyncCatalogHandler>()
                    .HandleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while syncing the catalog with Comic Vine.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}

public sealed class CatalogSyncOptions
{
    public const string SectionName = "CatalogSync";

    public bool Enabled { get; set; } = true;

    public int InitialDelayMinutes { get; set; } = 2;

    public int IntervalHours { get; set; } = 24;
}
