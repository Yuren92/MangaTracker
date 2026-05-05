using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class SeriesConfiguration : IEntityTypeConfiguration<Series>
{
    public void Configure(EntityTypeBuilder<Series> builder)
    {
        builder.ToTable("Series");

        builder.HasKey(series => series.Id);

        builder.Property(series => series.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(series => series.OriginalTitle)
            .HasMaxLength(300);

        builder.Property(series => series.Description)
            .HasMaxLength(4000);

        builder.Property(series => series.ImageUrl)
            .HasMaxLength(1000);

        builder.Property(series => series.CreatedAt)
            .IsRequired();

        builder.Property(series => series.UpdatedAt);

        builder
            .HasMany(series => series.Editions)
            .WithOne(edition => edition.Series)
            .HasForeignKey(edition => edition.SeriesId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(series => series.Editions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}