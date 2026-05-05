using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.Entities;

public sealed class Series
{
    private readonly List<Edition> _editions = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? OriginalTitle { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<Edition> Editions => _editions.AsReadOnly();

    private Series()
    {
        Title = string.Empty;
    }

    public Series(
        string title,
        string? originalTitle = null,
        string? description = null,
        string? imageUrl = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Series title is required.");
        }

        Id = Guid.NewGuid();
        Title = title.Trim();
        OriginalTitle = NormalizeOptionalText(originalTitle);
        Description = NormalizeOptionalText(description);
        ImageUrl = NormalizeOptionalText(imageUrl);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(
        string title,
        string? originalTitle,
        string? description,
        string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Series title is required.");
        }

        Title = title.Trim();
        OriginalTitle = NormalizeOptionalText(originalTitle);
        Description = NormalizeOptionalText(description);
        ImageUrl = NormalizeOptionalText(imageUrl);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}