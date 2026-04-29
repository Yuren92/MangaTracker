using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Persistence;

public sealed class MangaTrackerDbContext : DbContext
{
    public MangaTrackerDbContext(DbContextOptions<MangaTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<MangaCollectionItem> MangaCollectionItems => Set<MangaCollectionItem>();

    public DbSet<User> Users => Set<User>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MangaTrackerDbContext).Assembly);
    }
}