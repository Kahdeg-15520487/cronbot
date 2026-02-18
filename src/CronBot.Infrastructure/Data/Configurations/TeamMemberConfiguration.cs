using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CronBot.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="TeamMember"/>.
/// </summary>
public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members");

        builder.HasKey(tm => new { tm.TeamId, tm.UserId });

        builder.Property(tm => tm.Role)
            .HasDefaultValue(TeamRole.Member);

        builder.Property(tm => tm.JoinedAt)
            .HasDefaultValueSql("NOW()");

        // Relationships
        builder.HasOne(tm => tm.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
