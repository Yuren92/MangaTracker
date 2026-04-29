namespace MangaTracker.Application.Auth.ConfirmEmail;

public sealed record ConfirmEmailCommand(
    string Token
);