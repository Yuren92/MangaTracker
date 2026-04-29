using MangaTracker.Application.Abstractions.Auth;
using Microsoft.Extensions.Options;

namespace MangaTracker.Infrastructure.Auth;

public sealed class AuthLinkBuilder : IAuthLinkBuilder
{
    private readonly AuthLinkOptions _options;

    public AuthLinkBuilder(IOptions<AuthLinkOptions> options)
    {
        _options = options.Value;
    }

    public string BuildEmailConfirmationUrl(string token)
    {
        return $"{_options.FrontendBaseUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
    }

    public string BuildPasswordResetUrl(string token)
    {
        return $"{_options.FrontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
    }
}