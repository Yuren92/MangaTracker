namespace MangaTracker.Application.Auth.ForgotPassword;

public sealed record ForgotPasswordCommand(
    string Email
);