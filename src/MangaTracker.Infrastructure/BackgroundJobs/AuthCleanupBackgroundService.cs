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
        using var scope = _serviceScopeFactory.CreateScope();

        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var userTokenRepository = scope.ServiceProvider.GetRequiredService<IUserTokenRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var unconfirmedUsersCutoff = DateTimeOffset.UtcNow
            .AddHours(-_options.DeleteUnconfirmedUsersAfterHours);

        await userTokenRepository.DeleteExpiredOrUsedTokensAsync(cancellationToken);

        await userRepository.DeleteUnconfirmedUsersOlderThanAsync(
            unconfirmedUsersCutoff,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auth cleanup completed. Deleted expired/used tokens and unconfirmed users older than {Cutoff}.",
            unconfirmedUsersCutoff);
    }
}