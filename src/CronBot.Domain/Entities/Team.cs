namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a team that groups users and projects.
/// </summary>
public class Team : AuditableEntity
{
    /// <summary>
    /// Name of the team.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug for the team.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Description of the team.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a personal team (one per user).
    /// </summary>
    public bool IsPersonal { get; set; }

    /// <summary>
    /// ID of the team owner.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Team settings as JSON.
    /// </summary>
    public string Settings { get; set; } = "{}";

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
