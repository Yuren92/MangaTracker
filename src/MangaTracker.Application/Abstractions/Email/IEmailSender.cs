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

    // Sent instead of a confirmation when someone registers with an email that already
    // has an account, so the API response can stay the same in both cases.
    Task SendRegistrationAttemptForExistingAccountAsync(
        string to,
        string forgotPasswordUrl,
        CancellationToken cancellationToken = default);
}
