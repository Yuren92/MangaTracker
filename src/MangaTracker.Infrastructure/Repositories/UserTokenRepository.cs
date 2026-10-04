using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Domain.Enums;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class UserTokenRepository : IUserTokenRepository
{
    private readonly MangaTrackerDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public UserTokenRepository(
        MangaTrackerDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task<UserToken?> GetActiveTokenAsync(
        string tokenHash,
        UserTokenType type,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        return _dbContext.UserTokens
            .FirstOrDefaultAsync(
                token =>
                    token.TokenHash == tokenHash &&
                    token.Type == type &&
                    token.UsedAt == null &&
                    token.ExpiresAt > now,
                cancellationToken);
    }

    public async Task AddAsync(
        UserToken token,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.UserTokens.AddAsync(token, cancellationToken);
    }

    public async Task DeleteExpiredOrUsedTokensAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var tokensToDelete = await _dbContext.UserTokens
            .Where(token => token.UsedAt != null || token.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        _dbContext.UserTokens.RemoveRange(tokensToDelete);
    }

    public async Task MarkActiveTokensAsUsedAsync(
        Guid userId,
        UserTokenType type,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var activeTokens = await _dbContext.UserTokens
            .Where(token =>
                token.UserId == userId &&
                token.Type == type &&
                token.UsedAt == null &&
                token.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.MarkAsUsed();
        }
    }
}