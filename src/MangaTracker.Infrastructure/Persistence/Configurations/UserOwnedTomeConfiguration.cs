using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class UserOwnedTomeConfiguration : IEntityTypeConfiguration<UserOwnedTome>
{
    public void Configure(EntityTypeBuilder<UserOwnedTome> builder)
    {
        builder.ToTable("UserOwnedTomes");

        builder.HasKey(userOwnedTome => userOwnedTome.Id);
        builder.Property(userOwnedTome => userOwnedTome.Id)
            .ValueGeneratedNever();

        builder.Property(userOwnedTome => userOwnedTome.UserCollectionId)
            .IsRequired();

        builder.Property(userOwnedTome => userOwnedTome.TomeId)
            .IsRequired();

        builder.HasIndex(userOwnedTome => new
        {
            userOwnedTome.UserCollectionId,
            userOwnedTome.TomeId
        })
            .IsUnique();

        builder.Property(userOwnedTome => userOwnedTome.CreatedAt)
            .IsRequired();
    }
}