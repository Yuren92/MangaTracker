namespace MangaTracker.Application.Abstractions.Auth;

public interface ITokenGenerator
{
    string GenerateSecureToken();
}