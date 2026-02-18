using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskEntity = CronBot.Domain.Entities.Task;
using TaskStatus = CronBot.Domain.Enums.TaskStatus;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="TaskEntity"/>.
/// </summary>
public class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("tasks");

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.ProjectId);

        builder.HasIndex(t => t.SprintId);

        builder.HasIndex(t => t.Status);

        builder.HasIndex(t => new { t.AssigneeType, t.AssigneeId });

        builder.HasIndex(t => t.ParentTaskId);

        builder.HasAlternateKey(t => new { t.ProjectId, t.Number });

        builder.Property(t => t.Number)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Type)
            .HasDefaultValue(TaskType.Task);

        builder.Property(t => t.Status)
            .HasDefaultValue(TaskStatus.Backlog);

        builder.Property(t => t.ColumnOrder)
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Sprint)
            .WithMany(s => s.Tasks)
            .HasForeignKey(t => t.SprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.Board)
            .WithMany(b => b.Tasks)
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Comments)
            .WithOne(c => c.Task)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
