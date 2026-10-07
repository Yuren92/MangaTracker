using System.Threading.Channels;
using MangaTracker.Application.Abstractions.Email;

namespace MangaTracker.Infrastructure.Email;

// The IEmailSender the use cases get. It only queues the email and returns, so:
//  - an endpoint answers in the same time whether or not it sends an email, and
//    forgot-password or resend-confirmation cannot be timed to find registered emails;
//  - a slow or failing SMTP server never fails a request whose data is already saved
//    (for example a confirmed account whose welcome email could not be sent).
// EmailDispatcherService sends the queued emails with the real sender.
//
// The queue lives in memory: an email queued right before the process stops is lost
// (the user can ask for another link). An outbox table would make it durable.
public sealed class BackgroundEmailSender : IEmailSender
{
    private readonly ChannelWriter<EmailWorkItem> _queue;

    public BackgroundEmailSender(Channel<EmailWorkItem> queue)
    {
        _queue = queue.Writer;
    }

    public Task SendEmailConfirmationAsync(
        string to,
        string confirmationUrl,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            "email confirmation",
            (sender, token) => sender.SendEmailConfirmationAsync(to, confirmationUrl, token));
    }

    public Task SendPasswordResetAsync(
        string to,
        string resetPasswordUrl,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            "password reset",
            (sender, token) => sender.SendPasswordResetAsync(to, resetPasswordUrl, token));
    }

    public Task SendWelcomeAsync(
        string to,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            "welcome",
            (sender, token) => sender.SendWelcomeAsync(to, token));
    }

    public Task SendRegistrationAttemptForExistingAccountAsync(
        string to,
        string forgotPasswordUrl,
        CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(
            "registration attempt",
            (sender, token) => sender.SendRegistrationAttemptForExistingAccountAsync(to, forgotPasswordUrl, token));
    }

    // The request's cancellation token is deliberately not passed on: once the data is
    // saved, the email must go out even if the client disconnects.
    private Task EnqueueAsync(string kind, Func<IEmailSender, CancellationToken, Task> send)
    {
        return _queue.WriteAsync(new EmailWorkItem(kind, send)).AsTask();
    }
}

public sealed record EmailWorkItem(
    string Kind,
    Func<IEmailSender, CancellationToken, Task> Send);
