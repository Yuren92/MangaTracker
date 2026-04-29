namespace MangaTracker.Infrastructure.Auth;

public sealed class AuthCleanupOptions
{
    public int IntervalHours { get; set; } = 6;

    public int DeleteUnconfirmedUsersAfterHours { get; set; } = 48;
}