using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Sprint"/>.
/// </summary>
public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("sprints");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.ProjectId);

        builder.HasIndex(s => s.Status);

        builder.HasAlternateKey(s => new { s.ProjectId, s.Number });

        builder.Property(s => s.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasDefaultValue(SprintStatus.Planning);

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(s => s.Project)
            .WithMany(p => p.Sprints)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Tasks)
            .WithOne(t => t.Sprint)
            .HasForeignKey(t => t.SprintId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
