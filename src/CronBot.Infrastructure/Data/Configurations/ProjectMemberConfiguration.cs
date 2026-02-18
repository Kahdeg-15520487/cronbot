using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="ProjectMember"/>.
/// </summary>
public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");

        builder.HasKey(pm => new { pm.ProjectId, pm.UserId });

        builder.Property(pm => pm.Role)
            .HasDefaultValue(ProjectRole.Contributor);

        builder.Property(pm => pm.AddedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
