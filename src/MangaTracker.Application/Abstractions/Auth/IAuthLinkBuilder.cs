namespace MangaTracker.Application.Abstractions.Auth;

public interface IAuthLinkBuilder
{
    string BuildEmailConfirmationUrl(string token);

    string BuildPasswordResetUrl(string token);
}