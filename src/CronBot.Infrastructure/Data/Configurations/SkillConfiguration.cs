using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Skill"/>.
/// </summary>
public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("skills");

        builder.HasKey(s => s.Id);

        builder.HasIndex(s => s.Scope);

        builder.HasIndex(s => s.TeamId);

        builder.HasIndex(s => s.ProjectId);

        builder.Property(s => s.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Version)
            .HasMaxLength(50)
            .HasDefaultValue("1.0.0");

        builder.Property(s => s.FilePath)
            .IsRequired();

        builder.Property(s => s.Meta)
            .HasDefaultValue("{}");

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(s => s.Team)
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
