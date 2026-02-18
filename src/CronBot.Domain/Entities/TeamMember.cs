using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a user's membership in a team.
/// </summary>
public class TeamMember
{
    /// <summary>
    /// ID of the team.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// ID of the user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role of the user in the team.
    /// </summary>
    public TeamRole Role { get; set; } = TeamRole.Member;

    /// <summary>
    /// When the user joined the team.
    /// </summary>
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
