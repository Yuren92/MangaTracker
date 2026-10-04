namespace MangaTracker.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    string HashPassword(string password);

    PasswordVerificationResult VerifyPassword(
        string password,
        string passwordHash);
}

public enum PasswordVerificationResult
{
    Failed,
    Success,

    // Correct password, but the stored hash uses older parameters (for example fewer
    // PBKDF2 iterations) and should be replaced by a fresh hash.
    SuccessRehashNeeded
}
