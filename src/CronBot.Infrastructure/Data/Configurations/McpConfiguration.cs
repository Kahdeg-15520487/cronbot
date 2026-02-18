using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Mcp"/>.
/// </summary>
public class McpConfiguration : IEntityTypeConfiguration<Mcp>
{
    public void Configure(EntityTypeBuilder<Mcp> builder)
    {
        builder.ToTable("mcps");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.GroupType);

        builder.HasIndex(m => m.TeamId);

        builder.HasIndex(m => m.ProjectId);

        builder.Property(m => m.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.Version)
            .HasMaxLength(50)
            .HasDefaultValue("1.0.0");

        builder.Property(m => m.ContainerImage)
            .HasMaxLength(500);

        builder.Property(m => m.KomodoStackId)
            .HasMaxLength(255);

        builder.Property(m => m.Transport)
            .HasDefaultValue(McpTransport.Http);

        builder.Property(m => m.Command)
            .HasMaxLength(500);

        builder.Property(m => m.Url)
            .HasMaxLength(500);

        builder.Property(m => m.EnvironmentVars)
            .HasDefaultValue("{}");

        builder.Property(m => m.RequiredConfig)
            .HasDefaultValue("[]");

        builder.Property(m => m.Tools)
            .HasDefaultValue("[]");

        builder.Property(m => m.Resources)
            .HasDefaultValue("[]");

        builder.Property(m => m.Prompts)
            .HasDefaultValue("[]");

        builder.Property(m => m.Status)
            .HasDefaultValue(McpStatus.Registered);

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(m => m.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(m => m.Team)
            .WithMany()
            .HasForeignKey(m => m.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Project)
            .WithMany()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
