using System.Threading.Channels;
using MangaTracker.Application.Abstractions.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MangaTracker.Infrastructure.Email;

// Sends the emails queued by BackgroundEmailSender, one at a time, with the transport
// registered as EmailTransport (SMTP, or the console sender in Development).
public sealed class EmailDispatcherService : BackgroundService
{
    private readonly ChannelReader<EmailWorkItem> _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<EmailDispatcherService> _logger;

    public EmailDispatcherService(
        Channel<EmailWorkItem> queue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<EmailDispatcherService> logger)
    {
        _queue = queue.Reader;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var transport = scope.ServiceProvider.GetRequiredKeyedService<IEmailSender>(EmailTransport.Key);

                await workItem.Send(transport, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // One failed email must not stop the ones queued after it.
                _logger.LogError(exception, "Could not send the {EmailKind} email", workItem.Kind);
            }
        }
    }
}

public static class EmailTransport
{
    // Keyed registration of the sender that really delivers (SMTP or console).
    public const string Key = "transport";
}
