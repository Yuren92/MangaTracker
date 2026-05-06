using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MangaTracker.Infrastructure.Persistence;

public sealed class MangaTrackerDbContext : DbContext
{
    public MangaTrackerDbContext(DbContextOptions<MangaTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    public DbSet<Series> Series => Set<Series>();
    public DbSet<Edition> Editions => Set<Edition>();
    public DbSet<Tome> Tomes => Set<Tome>();
    public DbSet<UserCollection> UserCollections => Set<UserCollection>();
    public DbSet<UserOwnedTome> UserOwnedTomes => Set<UserOwnedTome>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MangaTrackerDbContext).Assembly);
    }
}