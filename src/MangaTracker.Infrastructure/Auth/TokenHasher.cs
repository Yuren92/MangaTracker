using System.Security.Cryptography;
using System.Text;
using MangaTracker.Application.Abstractions.Auth;

namespace MangaTracker.Infrastructure.Auth;

public sealed class TokenHasher : ITokenHasher
{
    public string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}