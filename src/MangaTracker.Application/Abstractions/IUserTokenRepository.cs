using MangaTracker.Domain.Entities;
using MangaTracker.Domain.Enums;

namespace MangaTracker.Application.Abstractions;

public interface IUserTokenRepository
{
    Task<UserToken?> GetActiveTokenAsync(
        string tokenHash,
        UserTokenType type,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserToken token,
        CancellationToken cancellationToken = default);

    Task DeleteExpiredOrUsedTokensAsync(
        CancellationToken cancellationToken = default);

    Task MarkActiveTokensAsUsedAsync(
        Guid userId,
        UserTokenType type,
        CancellationToken cancellationToken = default);
}