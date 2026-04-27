using MangaTracker.Application.Abstractions.Auth;

namespace MangaTracker.Api.Auth;

public sealed class DemoCurrentUserService : ICurrentUserService
{
    public Guid UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
}