using MangaTracker.Application.Abstractions.Email;
using Microsoft.Extensions.Logging;

namespace MangaTracker.Infrastructure.Email;

public sealed class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailConfirmationAsync(
        string to,
        string confirmationUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email confirmation for {Email}: {ConfirmationUrl}",
            to,
            confirmationUrl);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(
        string to,
        string resetPasswordUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Password reset for {Email}: {ResetPasswordUrl}",
            to,
            resetPasswordUrl);

        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(
        string to,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Welcome email sent to {Email}",
            to);

        return Task.CompletedTask;
    }

    public Task SendRegistrationAttemptForExistingAccountAsync(
        string to,
        string forgotPasswordUrl,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Registration attempt for existing account {Email}",
            to);

        return Task.CompletedTask;
    }
}
