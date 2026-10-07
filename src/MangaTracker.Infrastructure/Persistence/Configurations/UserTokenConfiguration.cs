using MangaTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaTracker.Infrastructure.Persistence.Configurations;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(token => token.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(token => token.CreatedAt)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .IsRequired();

        // Optimistic concurrency: marking a token as used updates it WHERE UsedAt is still
        // NULL, so two requests racing with the same single-use token cannot both succeed.
        builder.Property(token => token.UsedAt)
            .IsConcurrencyToken();

        builder.HasIndex(token => new { token.TokenHash, token.Type });

        builder.HasIndex(token => token.UserId);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}