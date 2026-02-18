using CronBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Team"/>.
/// </summary>
public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.HasIndex(t => t.OwnerId);

        builder.Property(t => t.Name)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.IsPersonal)
            .HasDefaultValue(false);

        builder.Property(t => t.Settings)
            .HasDefaultValue("{}");

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(t => t.Owner)
            .WithMany()
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Members)
            .WithOne(tm => tm.Team)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Projects)
            .WithOne(p => p.Team)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
