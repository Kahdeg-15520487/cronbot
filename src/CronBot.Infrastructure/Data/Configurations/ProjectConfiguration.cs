using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Project"/>.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.TeamId);

        builder.HasIndex(p => p.Slug);

        builder.HasAlternateKey(p => new { p.TeamId, p.Slug });

        builder.Property(p => p.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.GitMode)
            .HasDefaultValue(GitMode.Internal);

        builder.Property(p => p.AutonomyLevel)
            .HasDefaultValue(1);

        builder.Property(p => p.SandboxMode)
            .HasDefaultValue(SandboxMode.Standard);

        builder.Property(p => p.MaxAgents)
            .HasDefaultValue(3);

        builder.Property(p => p.Settings)
            .HasDefaultValue("{}");

        builder.Property(p => p.ScalingConfig)
            .HasDefaultValue("{}");

        builder.Property(p => p.ErrorHandlingConfig)
            .HasDefaultValue("{}");

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(p => p.Team)
            .WithMany(t => t.Projects)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Members)
            .WithOne(pm => pm.Project)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Boards)
            .WithOne(b => b.Project)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Sprints)
            .WithOne(s => s.Project)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Agents)
            .WithOne(a => a.Project)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
