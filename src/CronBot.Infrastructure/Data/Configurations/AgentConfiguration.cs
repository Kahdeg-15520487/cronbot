using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Agent"/>.
/// </summary>
public class AgentConfiguration : IEntityTypeConfiguration<Agent>
{
    public void Configure(EntityTypeBuilder<Agent> builder)
    {
        builder.ToTable("agents");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.ProjectId);

        builder.HasIndex(a => a.Status);

        builder.HasIndex(a => a.CurrentTaskId);

        builder.Property(a => a.ContainerId)
            .HasMaxLength(255);

        builder.Property(a => a.ContainerName)
            .HasMaxLength(255);

        builder.Property(a => a.Status)
            .HasDefaultValue(AgentStatus.Idle);

        builder.Property(a => a.StartedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.TasksCompleted)
            .HasDefaultValue(0);

        builder.Property(a => a.CommitsMade)
            .HasDefaultValue(0);

        // Relationships
        builder.HasOne(a => a.Project)
            .WithMany(p => p.Agents)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.CurrentTask)
            .WithMany()
            .HasForeignKey(a => a.CurrentTaskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
