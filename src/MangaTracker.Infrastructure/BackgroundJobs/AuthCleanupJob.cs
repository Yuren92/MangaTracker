using MangaTracker.Application.Abstractions;
using MangaTracker.Infrastructure.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MangaTracker.Infrastructure.BackgroundJobs;

// One cleanup pass: deletes used or expired tokens and accounts that were never
// confirmed within the configured window. Separate from the hosted service so it can
// be run and tested on its own, with an injected clock.
public sealed class AuthCleanupJob
{
    private readonly IUserRepository _userRepository;
    private readonly IUserTokenRepository _userTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly AuthCleanupOptions _options;
    private readonly ILogger<AuthCleanupJob> _logger;

    public AuthCleanupJob(
        IUserRepository userRepository,
        IUserTokenRepository userTokenRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOptions<AuthCleanupOptions> options,
        ILogger<AuthCleanupJob> logger)
    {
        _userRepository = userRepository;
        _userTokenRepository = userTokenRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var unconfirmedUsersCutoff = _timeProvider.GetUtcNow()
            .AddHours(-_options.DeleteUnconfirmedUsersAfterHours);

        await _userTokenRepository.DeleteExpiredOrUsedTokensAsync(cancellationToken);

        await _userRepository.DeleteUnconfirmedUsersOlderThanAsync(
            unconfirmedUsersCutoff,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Auth cleanup completed. Deleted expired/used tokens and unconfirmed users older than {Cutoff}.",
            unconfirmedUsersCutoff);
    }
}
