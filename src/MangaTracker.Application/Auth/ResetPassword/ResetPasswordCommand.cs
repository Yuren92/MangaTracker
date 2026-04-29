namespace MangaTracker.Application.Auth.ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword
);