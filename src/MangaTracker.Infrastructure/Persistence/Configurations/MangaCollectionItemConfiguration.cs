using MangaTracker.Domain.Entities;
using MangaTracker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class MangaCollectionItemConfiguration : IEntityTypeConfiguration<MangaCollectionItem>
{
    public void Configure(EntityTypeBuilder<MangaCollectionItem> builder)
    {
        builder.ToTable("MangaCollectionItems");

        builder.HasKey(manga => manga.Id);

        builder.Property(manga => manga.UserId)
            .IsRequired();

        builder.Property(manga => manga.MalId)
            .IsRequired();

        builder.HasIndex(manga => new { manga.UserId, manga.MalId })
            .IsUnique();

        builder.Property(manga => manga.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(manga => manga.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(manga => manga.MalTotalVolumes);

        builder.Property(manga => manga.CustomTotalVolumes);

        builder
            .HasMany(manga => manga.OwnedVolumes)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(manga => manga.OwnedVolumes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}