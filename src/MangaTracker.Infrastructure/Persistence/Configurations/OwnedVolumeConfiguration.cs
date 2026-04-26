using MangaTracker.Domain.Entities;
using MangaTracker.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class OwnedVolumeConfiguration : IEntityTypeConfiguration<OwnedVolume>
{
    public void Configure(EntityTypeBuilder<OwnedVolume> builder)
    {
        builder.ToTable("OwnedVolumes");

        builder.Property<Guid>("Id")
        .ValueGeneratedOnAdd();

        builder.HasKey("Id");

        builder.Property(volume => volume.Number)
            .HasConversion(
                volumeNumber => volumeNumber.Value,
                value => new VolumeNumber(value))
            .HasColumnName("VolumeNumber")
            .IsRequired();

        builder.Property(volume => volume.PurchaseDate);

        builder.Property(volume => volume.Price)
            .HasColumnType("decimal(10,2)");

        builder.Property(volume => volume.Store)
            .HasMaxLength(200);
    }
}