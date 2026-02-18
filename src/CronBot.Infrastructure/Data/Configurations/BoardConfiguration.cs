using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Board"/>.
/// </summary>
public class BoardConfiguration : IEntityTypeConfiguration<Board>
{
    public void Configure(EntityTypeBuilder<Board> builder)
    {
        builder.ToTable("boards");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(b => b.Columns)
            .HasDefaultValue("[\"Backlog\", \"Sprint\", \"In Progress\", \"Review\", \"Blocked\", \"Done\"]");

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(b => b.Project)
            .WithMany(p => p.Boards)
            .HasForeignKey(b => b.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Tasks)
            .WithOne(t => t.Board)
            .HasForeignKey(t => t.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
