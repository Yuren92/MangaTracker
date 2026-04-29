namespace MangaTracker.Application.Auth.RegisterUser;

public sealed record RegisterUserResult(
    Guid UserId,
    string Email
);