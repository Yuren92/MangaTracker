namespace MangaTracker.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string AuthSensitive = "auth-sensitive";
    public const string ExternalApi = "external-api";
    public const string ComicVineImport = "comic-vine-import";
}
