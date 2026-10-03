namespace MangaTracker.Application.Auth.ChangePassword;

// Changing the password rotates the security stamp, which revokes every access token
// issued before, including the caller's. AccessToken replaces it for this session.
public sealed record ChangePasswordResult(
    string Message,
    string AccessToken);
