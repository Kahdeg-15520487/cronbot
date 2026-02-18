using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Notification"/>.
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.HasIndex(n => n.UserId);

        builder.HasIndex(n => n.UserId)
            .HasFilter("\"ReadAt\" IS NULL")
            .HasDatabaseName("idx_notifications_unread");

        builder.HasIndex(n => n.CreatedAt);

        builder.Property(n => n.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(n => n.Message)
            .IsRequired();

        builder.Property(n => n.Data)
            .HasDefaultValue("{}");

        builder.Property(n => n.Channel)
            .HasDefaultValue(NotificationChannel.Web);

        builder.Property(n => n.Priority)
            .HasDefaultValue(NotificationPriority.Normal);

        builder.Property(n => n.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Project)
            .WithMany()
            .HasForeignKey(n => n.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
