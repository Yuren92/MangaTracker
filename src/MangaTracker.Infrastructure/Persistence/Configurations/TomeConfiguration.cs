using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class TomeConfiguration : IEntityTypeConfiguration<Tome>
{
    public void Configure(EntityTypeBuilder<Tome> builder)
    {
        builder.ToTable("Tomes");

        builder.HasKey(tome => tome.Id);

        builder.Property(tome => tome.EditionId)
            .IsRequired();

        builder.Property(tome => tome.ComicVineIssueId)
            .IsRequired();

        builder.HasIndex(tome => tome.ComicVineIssueId)
            .IsUnique();

        builder.Property(tome => tome.ComicVineApiDetailUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(tome => tome.ComicVineApiDetailUrl)
            .IsUnique();

        builder.HasIndex(tome => new { tome.EditionId, tome.IssueNumber })
            .IsUnique();

        builder.Property(tome => tome.IssueNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(tome => tome.NormalizedNumber);

        builder.Property(tome => tome.Title)
            .HasMaxLength(300);

        builder.Property(tome => tome.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(tome => tome.CoverDate);

        builder.Property(tome => tome.StoreDate);

        builder.Property(tome => tome.SiteDetailUrl)
            .HasMaxLength(1000);

        builder.Property(tome => tome.CreatedAt)
            .IsRequired();

        builder.Property(tome => tome.UpdatedAt);

        builder
            .HasMany(tome => tome.UserOwnedTomes)
            .WithOne(userOwnedTome => userOwnedTome.Tome)
            .HasForeignKey(userOwnedTome => userOwnedTome.TomeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(tome => tome.UserOwnedTomes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}