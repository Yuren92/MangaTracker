using System.Security.Cryptography;
using MangaTracker.Application.Abstractions.Auth;

namespace MangaTracker.Infrastructure.Auth;

public sealed class TokenGenerator : ITokenGenerator
{
    public string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}