using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a sprint in a project.
/// </summary>
public class Sprint : AuditableEntity
{
    /// <summary>
    /// ID of the project this sprint belongs to.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Name of the sprint.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Sprint number within the project.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Sprint goal description.
    /// </summary>
    public string? Goal { get; set; }

    /// <summary>
    /// Current status of the sprint.
    /// </summary>
    public SprintStatus Status { get; set; } = SprintStatus.Planning;

    /// <summary>
    /// Start date of the sprint.
    /// </summary>
    public DateOnly? StartDate { get; set; }

    /// <summary>
    /// End date of the sprint.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// Velocity in story points (set after sprint completion).
    /// </summary>
    public int? VelocityPoints { get; set; }

    /// <summary>
    /// Notes from the sprint retrospective.
    /// </summary>
    public string? RetrospectiveNotes { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public ICollection<Task> Tasks { get; set; } = [];
}
