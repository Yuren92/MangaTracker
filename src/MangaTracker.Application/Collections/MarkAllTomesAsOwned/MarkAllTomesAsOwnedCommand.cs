namespace MangaTracker.Application.Collections.MarkAllTomesAsOwned;

public sealed record MarkAllTomesAsOwnedCommand(
    Guid UserId,
    Guid CollectionId);