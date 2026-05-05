using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.Entities;

public sealed class UserOwnedTome
{
    public Guid Id { get; private set; }
    public Guid UserCollectionId { get; private set; }
    public UserCollection UserCollection { get; private set; } = null!;
    public Guid TomeId { get; private set; }
    public Tome Tome { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private UserOwnedTome()
    {
    }

    public UserOwnedTome(
        Guid userCollectionId,
        Guid tomeId)
    {
        if (userCollectionId == Guid.Empty)
        {
            throw new DomainException("User collection id is required.");
        }

        if (tomeId == Guid.Empty)
        {
            throw new DomainException("Tome id is required.");
        }

        Id = Guid.NewGuid();
        UserCollectionId = userCollectionId;
        TomeId = tomeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}