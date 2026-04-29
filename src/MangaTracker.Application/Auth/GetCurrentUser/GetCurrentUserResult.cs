namespace MangaTracker.Application.Auth.GetCurrentUser;

public sealed record GetCurrentUserResult(
    Guid UserId,
    string Email
);