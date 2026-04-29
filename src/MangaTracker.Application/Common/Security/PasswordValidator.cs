using MangaTracker.Application.Common.Exceptions;

namespace MangaTracker.Application.Common.Security;

public static class PasswordValidator
{
    private const int MinimumLength = 8;

    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        if (password.Length < MinimumLength)
        {
            throw new ValidationException($"Password must have at least {MinimumLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new ValidationException("Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            throw new ValidationException("Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new ValidationException("Password must contain at least one number.");
        }
    }
}