using MangaTracker.Application.Abstractions;
using MangaTracker.Domain.Entities;
using MangaTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly MangaTrackerDbContext _dbContext;

    public UserRepository(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    public Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUnconfirmedUsersOlderThanAsync(
    DateTimeOffset cutoff,
    CancellationToken cancellationToken = default)
    {
        var usersToDelete = await _dbContext.Users
            .Where(user =>
                !user.IsEmailConfirmed &&
                user.CreatedAt < cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.Users.RemoveRange(usersToDelete);
    }
}