using MangaTracker.Domain.Common;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Domain.Entities;

public sealed class UserToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public UserTokenType Type { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private UserToken()
    {
        TokenHash = string.Empty;
    }

    public UserToken(
        Guid userId,
        string tokenHash,
        UserTokenType type,
        DateTimeOffset expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Token hash is required.");
        }

        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new DomainException("Token expiration must be in the future.");
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        Type = type;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        UsedAt = null;
    }

    public bool IsUsed => UsedAt is not null;

    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    public bool IsActive => !IsUsed && !IsExpired;

    public void MarkAsUsed()
    {
        if (IsUsed)
        {
            return;
        }

        UsedAt = DateTimeOffset.UtcNow;
    }
}