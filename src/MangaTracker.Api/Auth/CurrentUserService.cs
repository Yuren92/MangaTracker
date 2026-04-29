using System.Security.Claims;
using MangaTracker.Application.Abstractions.Auth;

namespace MangaTracker.Api.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var userIdValue = _httpContextAccessor
                .HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userIdValue))
            {
                throw new InvalidOperationException("User id was not found in the current context.");
            }

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                throw new InvalidOperationException("User id is not a valid Guid.");
            }

            return userId;
        }
    }
}