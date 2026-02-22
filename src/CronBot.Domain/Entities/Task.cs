using CronBot.Domain.Enums;
using TaskStatus = CronBot.Domain.Enums.TaskStatus;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a task in the Kanban system.
/// </summary>
public class Task : AuditableEntity
{
    /// <summary>
    /// ID of the project this task belongs to.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// ID of the sprint this task is in (optional).
    /// </summary>
    public Guid? SprintId { get; set; }

    /// <summary>
    /// ID of the board this task is on.
    /// </summary>
    public Guid BoardId { get; set; }

    /// <summary>
    /// Task number within the project (auto-generated).
    /// </summary>
    public long Number { get; set; }

    /// <summary>
    /// Title of the task.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the task.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of the task.
    /// </summary>
    public TaskType Type { get; set; } = TaskType.Task;

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Backlog;

    /// <summary>
    /// Story points estimate (optional).
    /// </summary>
    public int? StoryPoints { get; set; }

    /// <summary>
    /// Type of assignee (User or Agent).
    /// </summary>
    public string? AssigneeType { get; set; }

    /// <summary>
    /// ID of the assignee.
    /// </summary>
    public Guid? AssigneeId { get; set; }

    /// <summary>
    /// ID of the parent task (for epics).
    /// </summary>
    public Guid? ParentTaskId { get; set; }

    /// <summary>
    /// Git branch name for this task.
    /// </summary>
    public string? GitBranch { get; set; }

    /// <summary>
    /// Git pull request ID.
    /// </summary>
    public int? GitPrId { get; set; }

    /// <summary>
    /// Git pull request URL.
    /// </summary>
    public string? GitPrUrl { get; set; }

    /// <summary>
    /// Commit hash if merged.
    /// </summary>
    public string? GitMergedCommit { get; set; }

    /// <summary>
    /// ID of the task blocking this one.
    /// </summary>
    public Guid? BlockedByTaskId { get; set; }

    /// <summary>
    /// Reason why this task is blocked.
    /// </summary>
    public string? BlockedReason { get; set; }

    /// <summary>
    /// Order position within the column.
    /// </summary>
    public int ColumnOrder { get; set; }

    /// <summary>
    /// When work started on this task.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// When this task was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public Sprint? Sprint { get; set; }
    public Board Board { get; set; } = null!;
    public Task? ParentTask { get; set; }
    public ICollection<Task> SubTasks { get; set; } = [];
    public ICollection<TaskComment> Comments { get; set; } = [];
    public ICollection<TaskLog> Logs { get; set; } = [];
}
