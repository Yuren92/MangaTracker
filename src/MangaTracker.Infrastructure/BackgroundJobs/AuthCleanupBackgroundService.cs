using MangaTracker.Application.Abstractions;
using MangaTracker.Infrastructure.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangaTracker.Infrastructure.BackgroundJobs;

public sealed class AuthCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AuthCleanupBackgroundService> _logger;
    private readonly AuthCleanupOptions _options;

    public AuthCleanupBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<AuthCleanupBackgroundService> logger,
        IOptions<AuthCleanupOptions> options)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_options.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while running auth cleanup.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        // A fresh scope per run: the job uses scoped services (DbContext, repositories).
        using var scope = _serviceScopeFactory.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<AuthCleanupJob>()
            .RunAsync(cancellationToken);
    }
}
