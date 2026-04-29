namespace MangaTracker.Application.Auth.LoginUser;

public sealed record LoginUserCommand(
    string Email,
    string Password
);