namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a Kanban board for a project.
/// </summary>
public class Board : Entity
{
    /// <summary>
    /// ID of the project this board belongs to.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Name of the board.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Column configuration as JSON array.
    /// </summary>
    public string Columns { get; set; } = "[\"Backlog\", \"Sprint\", \"In Progress\", \"Review\", \"Blocked\", \"Done\"]";

    /// <summary>
    /// When the board was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Project Project { get; set; } = null!;
    public ICollection<Task> Tasks { get; set; } = [];
}
