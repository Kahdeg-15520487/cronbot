using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a user's membership in a project.
/// </summary>
public class ProjectMember
{
    /// <summary>
    /// ID of the project.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// ID of the user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Role of the user in the project.
    /// </summary>
    public ProjectRole Role { get; set; } = ProjectRole.Contributor;

    /// <summary>
    /// When the user was added to the project.
    /// </summary>
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
}
