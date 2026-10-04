using MangaTracker.Application.Abstractions.Auth;
using MangaTracker.Domain.Entities;
using IdentityPasswordHasher = Microsoft.AspNetCore.Identity.PasswordHasher<MangaTracker.Domain.Entities.User>;
using IdentityResult = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace MangaTracker.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly IdentityPasswordHasher _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(
            user: null!,
            password: password);
    }

    public PasswordVerificationResult VerifyPassword(
        string password,
        string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user: null!,
            hashedPassword: passwordHash,
            providedPassword: password);

        return result switch
        {
            IdentityResult.Success => PasswordVerificationResult.Success,
            IdentityResult.SuccessRehashNeeded => PasswordVerificationResult.SuccessRehashNeeded,
            _ => PasswordVerificationResult.Failed
        };
    }
}
