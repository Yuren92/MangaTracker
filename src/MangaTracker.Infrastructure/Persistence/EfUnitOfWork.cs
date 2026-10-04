using MangaTracker.Application.Abstractions;
using MangaTracker.Application.Common.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    // SQL Server: 2601 = duplicate key in a unique index, 2627 = unique/primary key constraint.
    private static readonly int[] UniqueViolationErrorNumbers = [2601, 2627];

    private readonly MangaTrackerDbContext _dbContext;

    public EfUnitOfWork(MangaTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException &&
                  UniqueViolationErrorNumbers.Contains(sqlException.Number))
        {
            throw new UniqueConstraintViolationException(
                "The data was modified by another request. Please try again.",
                exception);
        }
    }

    public void DiscardChanges()
    {
        _dbContext.ChangeTracker.Clear();
    }
}
