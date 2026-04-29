using MangaTracker.Domain.Entities;

namespace MangaTracker.Application.Abstractions.Auth;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}