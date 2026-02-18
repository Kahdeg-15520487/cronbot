using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a project that agents work on.
/// </summary>
public class Project : AuditableEntity
{
    /// <summary>
    /// ID of the team that owns this project.
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// Name of the project.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug for the project.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Description of the project.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Git repository mode.
    /// </summary>
    public GitMode GitMode { get; set; } = GitMode.Internal;

    /// <summary>
    /// External Git repository URL (if using external mode).
    /// </summary>
    public string? ExternalGitUrl { get; set; }

    /// <summary>
    /// Encrypted token for external Git access.
    /// </summary>
    public string? ExternalGitTokenEncrypted { get; set; }

    /// <summary>
    /// Internal Gitea repository ID.
    /// </summary>
    public int? InternalRepoId { get; set; }

    /// <summary>
    /// Internal Gitea repository URL.
    /// </summary>
    public string? InternalRepoUrl { get; set; }

    /// <summary>
    /// Agent autonomy level (0-3).
    /// </summary>
    public int AutonomyLevel { get; set; } = 1;

    /// <summary>
    /// Security sandbox mode.
    /// </summary>
    public SandboxMode SandboxMode { get; set; } = SandboxMode.Standard;

    /// <summary>
    /// Name of the template used to create this project.
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Komodo stack ID for this project's containers.
    /// </summary>
    public string? KomodoStackId { get; set; }

    /// <summary>
    /// Maximum number of concurrent agents.
    /// </summary>
    public int MaxAgents { get; set; } = 3;

    /// <summary>
    /// Project settings as JSON.
    /// </summary>
    public string Settings { get; set; } = "{}";

    /// <summary>
    /// Scaling configuration as JSON.
    /// </summary>
    public string ScalingConfig { get; set; } = "{}";

    /// <summary>
    /// Error handling configuration as JSON.
    /// </summary>
    public string ErrorHandlingConfig { get; set; } = "{}";

    /// <summary>
    /// Whether the project is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Team Team { get; set; } = null!;
    public ICollection<ProjectMember> Members { get; set; } = [];
    public ICollection<Board> Boards { get; set; } = [];
    public ICollection<Sprint> Sprints { get; set; } = [];
    public ICollection<Task> Tasks { get; set; } = [];
    public ICollection<Agent> Agents { get; set; } = [];
}
