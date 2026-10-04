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

    // Random value embedded in every access token. Rotating it invalidates all tokens
    // issued before, which is how a password change signs the user out everywhere.
    public string SecurityStamp { get; private set; }

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        SecurityStamp = string.Empty;
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
        SecurityStamp = NewSecurityStamp();
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
        SecurityStamp = NewSecurityStamp();
    }

    // Same password, stronger hash (e.g. more PBKDF2 iterations). Unlike a password
    // change, this must not rotate the security stamp: it would sign the user out of
    // every other session just for logging in.
    public void UpgradePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
    }

    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }
}