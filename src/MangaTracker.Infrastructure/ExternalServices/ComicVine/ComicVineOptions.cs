namespace MangaTracker.Infrastructure.ExternalServices.ComicVine;

public sealed class ComicVineOptions
{
    public const string SectionName = "ComicVine";

    public string BaseUrl { get; init; } = "https://comicvine.gamespot.com/api/";
    public string ApiKey { get; init; } = string.Empty;
}