using System.Security.Claims;

namespace MangaTracker.Api.RateLimiting;

public static class RateLimitPartitionKey
{
    // Authenticated requests are limited per user: the id comes from a signed token,
    // so it cannot be spoofed and does not depend on proxies in front of the API.
    // Anonymous requests (login, register, password reset) can only be limited per
    // client IP, which is correct only if forwarded headers are configured for the
    // proxies in front of the API.
    public static string For(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return $"ip:{ipAddress}";
    }
}
