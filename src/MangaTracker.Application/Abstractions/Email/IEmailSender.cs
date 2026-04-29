namespace MangaTracker.Application.Abstractions.Email;

public interface IEmailSender
{
    Task SendEmailConfirmationAsync(
        string to,
        string confirmationUrl,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string to,
        string resetPasswordUrl,
        CancellationToken cancellationToken = default);

    Task SendWelcomeAsync(
        string to,
        CancellationToken cancellationToken = default);
}