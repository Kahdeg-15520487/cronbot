using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for TaskLog entity.
/// </summary>
public class TaskLogConfiguration : IEntityTypeConfiguration<TaskLog>
{
    public void Configure(EntityTypeBuilder<TaskLog> builder)
    {
        builder.ToTable("task_logs");

        builder.HasKey(tl => tl.Id);

        builder.Property(tl => tl.TaskId)
            .IsRequired();

        builder.Property(tl => tl.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(tl => tl.Level)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(TaskLogLevel.Info);

        builder.Property(tl => tl.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(tl => tl.Details)
            .HasColumnType("text");

        builder.Property(tl => tl.Source)
            .HasMaxLength(100);

        builder.Property(tl => tl.GitCommit)
            .HasMaxLength(40);

        builder.Property(tl => tl.GitBranch)
            .HasMaxLength(200);

        builder.Property(tl => tl.FilesAffected)
            .HasColumnType("jsonb");

        builder.Property(tl => tl.Metadata)
            .HasColumnType("jsonb");

        // Index for quick lookup by task
        builder.HasIndex(tl => tl.TaskId);

        // Index for filtering by type
        builder.HasIndex(tl => tl.Type);

        // Index for chronological ordering
        builder.HasIndex(tl => tl.CreatedAt);

        // Relationship
        builder.HasOne(tl => tl.Task)
            .WithMany(t => t.Logs)
            .HasForeignKey(tl => tl.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
