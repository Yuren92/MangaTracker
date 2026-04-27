namespace MangaTracker.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    Guid UserId { get; }
}