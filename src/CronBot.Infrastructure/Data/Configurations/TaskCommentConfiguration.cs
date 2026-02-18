using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="TaskComment"/>.
/// </summary>
public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("task_comments");

        builder.HasKey(tc => tc.Id);

        builder.HasIndex(tc => tc.TaskId);

        builder.Property(tc => tc.AuthorType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(tc => tc.Content)
            .IsRequired();

        builder.Property(tc => tc.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(tc => tc.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(tc => tc.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(tc => tc.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
