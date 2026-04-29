namespace MangaTracker.Application.Auth.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password
);