namespace MangaTracker.Api.RateLimiting;

public static class RateLimitPolicies
{
    public const string AuthSensitive = "auth-sensitive";
    public const string ExternalApi = "external-api";
    public const string ComicVineImport = "comic-vine-import";

    // Separate from imports: the collections page syncs on every visit, and sharing a
    // bucket would let ordinary navigation use up the user's imports for the minute.
    public const string CollectionSync = "collection-sync";
}
