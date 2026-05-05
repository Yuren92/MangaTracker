using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class EditionConfiguration : IEntityTypeConfiguration<Edition>
{
    public void Configure(EntityTypeBuilder<Edition> builder)
    {
        builder.ToTable("Editions");

        builder.HasKey(edition => edition.Id);

        builder.Property(edition => edition.SeriesId)
            .IsRequired();

        builder.Property(edition => edition.ComicVineVolumeId)
            .IsRequired();

        builder.HasIndex(edition => edition.ComicVineVolumeId)
            .IsUnique();

        builder.Property(edition => edition.ComicVineApiDetailUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(edition => edition.ComicVineApiDetailUrl)
            .IsUnique();

        builder.Property(edition => edition.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(edition => edition.PublisherName)
            .HasMaxLength(200);

        builder.Property(edition => edition.StartYear);

        builder.Property(edition => edition.Description)
            .HasMaxLength(4000);

        builder.Property(edition => edition.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(edition => edition.SiteDetailUrl)
            .HasMaxLength(1000);

        builder.Property(edition => edition.IssueCount);

        builder.Property(edition => edition.ImportedAt)
            .IsRequired();

        builder.Property(edition => edition.LastSyncedAt);

        builder
            .HasMany(edition => edition.Tomes)
            .WithOne(tome => tome.Edition)
            .HasForeignKey(tome => tome.EditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(edition => edition.UserCollections)
            .WithOne(userCollection => userCollection.Edition)
            .HasForeignKey(userCollection => userCollection.EditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(edition => edition.Tomes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(edition => edition.UserCollections)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}