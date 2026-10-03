using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.Entities;

public sealed class UserCollection
{
    private readonly List<UserOwnedTome> _ownedTomes = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EditionId { get; private set; }
    public Edition Edition { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<UserOwnedTome> OwnedTomes => _ownedTomes.AsReadOnly();

    private UserCollection()
    {
    }

    public UserCollection(
        Guid userId,
        Guid editionId)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (editionId == Guid.Empty)
        {
            throw new DomainException("Edition id is required.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        EditionId = editionId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    // Takes the tome itself so the aggregate can enforce that only tomes of this
    // collection's edition can be owned, whoever the caller is.
    public void MarkTomeAsOwned(Tome tome)
    {
        ArgumentNullException.ThrowIfNull(tome);

        if (tome.EditionId != EditionId)
        {
            throw new DomainException("Tome does not belong to this collection.");
        }

        var tomeId = tome.Id;

        var alreadyOwned = _ownedTomes.Any(ownedTome =>
            ownedTome.TomeId == tomeId);

        if (alreadyOwned)
        {
            return;
        }

        _ownedTomes.Add(new UserOwnedTome(Id, tomeId));
    }

    public void UnmarkTomeAsOwned(Guid tomeId)
    {
        if (tomeId == Guid.Empty)
        {
            throw new DomainException("Tome id is required.");
        }

        var ownedTome = _ownedTomes.FirstOrDefault(owned =>
            owned.TomeId == tomeId);

        if (ownedTome is null)
        {
            return;
        }

        _ownedTomes.Remove(ownedTome);
    }
}