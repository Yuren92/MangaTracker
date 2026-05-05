using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class UserCollectionConfiguration : IEntityTypeConfiguration<UserCollection>
{
    public void Configure(EntityTypeBuilder<UserCollection> builder)
    {
        builder.ToTable("UserCollections");

        builder.HasKey(userCollection => userCollection.Id);

        builder.Property(userCollection => userCollection.UserId)
            .IsRequired();

        builder.Property(userCollection => userCollection.EditionId)
            .IsRequired();

        builder.HasIndex(userCollection => new
        {
            userCollection.UserId,
            userCollection.EditionId
        })
            .IsUnique();

        builder.Property(userCollection => userCollection.CreatedAt)
            .IsRequired();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(userCollection => userCollection.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(userCollection => userCollection.OwnedTomes)
            .WithOne(userOwnedTome => userOwnedTome.UserCollection)
            .HasForeignKey(userOwnedTome => userOwnedTome.UserCollectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(userCollection => userCollection.OwnedTomes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}