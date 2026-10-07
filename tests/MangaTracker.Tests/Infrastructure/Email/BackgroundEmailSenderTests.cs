using System.Threading.Channels;
using AwesomeAssertions;
using MangaTracker.Application.Abstractions.Email;
using MangaTracker.Infrastructure.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MangaTracker.Tests.Infrastructure.Email;

public sealed class BackgroundEmailSenderTests
{
    private readonly Channel<EmailWorkItem> _queue = Channel.CreateUnbounded<EmailWorkItem>();
    private readonly IEmailSender _transport = Substitute.For<IEmailSender>();

    [Fact]
    public async Task Queueing_an_email_should_not_wait_for_the_transport()
    {
        var sender = new BackgroundEmailSender(_queue);

        await sender.SendPasswordResetAsync("user@example.com", "https://app/reset?token=abc");

        _queue.Reader.Count.Should().Be(1);
        await _transport.DidNotReceiveWithAnyArgs().SendPasswordResetAsync(default!, default!, default);
    }

    [Fact]
    public async Task Dispatcher_should_send_queued_emails_and_keep_going_after_a_failure()
    {
        _transport
            .SendWelcomeAsync("broken@example.com", Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var sender = new BackgroundEmailSender(_queue);
        await sender.SendWelcomeAsync("broken@example.com");
        await sender.SendEmailConfirmationAsync("user@example.com", "https://app/confirm?token=abc");
        _queue.Writer.Complete();

        using var dispatcher = CreateDispatcher();
        await dispatcher.StartAsync(CancellationToken.None);
        await dispatcher.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        await _transport.Received(1).SendEmailConfirmationAsync(
            "user@example.com",
            "https://app/confirm?token=abc",
            Arg.Any<CancellationToken>());
    }

    private EmailDispatcherService CreateDispatcher()
    {
        var provider = new ServiceCollection()
            .AddKeyedScoped(EmailTransport.Key, (_, _) => _transport)
            .BuildServiceProvider();

        return new EmailDispatcherService(
            _queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EmailDispatcherService>.Instance);
    }
}
