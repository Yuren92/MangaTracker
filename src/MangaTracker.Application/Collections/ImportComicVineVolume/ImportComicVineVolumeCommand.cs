namespace MangaTracker.Application.Collections.ImportComicVineVolume;

public sealed record ImportComicVineVolumeCommand(
    Guid UserId,
    string ApiDetailUrl);