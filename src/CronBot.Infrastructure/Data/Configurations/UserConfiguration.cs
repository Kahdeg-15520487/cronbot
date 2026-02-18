using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="User"/>.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.TelegramId)
            .IsUnique();

        builder.HasIndex(u => u.Username)
            .IsUnique();

        builder.Property(u => u.Username)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255);

        builder.Property(u => u.AuthentikId)
            .HasMaxLength(255);

        builder.Property(u => u.DisplayName)
            .HasMaxLength(255);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(u => u.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasMany(u => u.TeamMemberships)
            .WithOne(tm => tm.User)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.ProjectMemberships)
            .WithOne(pm => pm.User)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Notifications)
            .WithOne(n => n.User)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
