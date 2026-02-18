namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a comment on a task.
/// </summary>
public class TaskComment : AuditableEntity
{
    /// <summary>
    /// ID of the task this comment belongs to.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Type of author (User or Agent).
    /// </summary>
    public string AuthorType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the author.
    /// </summary>
    public Guid AuthorId { get; set; }

    /// <summary>
    /// Content of the comment.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    // Navigation properties
    public Task Task { get; set; } = null!;
}
