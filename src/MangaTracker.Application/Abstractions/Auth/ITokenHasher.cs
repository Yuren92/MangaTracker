namespace MangaTracker.Application.Abstractions.Auth;

public interface ITokenHasher
{
    string HashToken(string token);
}