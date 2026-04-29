using MangaTracker.Domain.Common;

namespace MangaTracker.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public DateTimeOffset? EmailConfirmedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(
        string email,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        IsEmailConfirmed = false;
        EmailConfirmedAt = null;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmEmail()
    {
        if (IsEmailConfirmed)
        {
            return;
        }

        IsEmailConfirmed = true;
        EmailConfirmedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
    }
}