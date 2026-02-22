namespace CronBot.Domain.Entities;

/// <summary>
/// Represents a log entry for task activity.
/// Tracks agent actions, git commits, file changes, and status updates.
/// </summary>
public class TaskLog : AuditableEntity
{
    /// <summary>
    /// ID of the task this log belongs to.
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// Type of log entry.
    /// </summary>
    public TaskLogType Type { get; set; }

    /// <summary>
    /// Severity level.
    /// </summary>
    public TaskLogLevel Level { get; set; }

    /// <summary>
    /// Brief summary of the activity.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Detailed content (JSON or formatted text).
    /// Can contain diff, commit details, error stack, etc.
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Source of the log entry (agent ID, user ID, system).
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Git commit hash if relevant.
    /// </summary>
    public string? GitCommit { get; set; }

    /// <summary>
    /// Git branch name if relevant.
    /// </summary>
    public string? GitBranch { get; set; }

    /// <summary>
    /// Files affected (JSON array of file paths).
    /// </summary>
    public string? FilesAffected { get; set; }

    /// <summary>
    /// Additional metadata (JSON object).
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    public Task Task { get; set; } = null!;
}

/// <summary>
/// Types of task log entries.
/// </summary>
public enum TaskLogType
{
    /// <summary>
    /// Task status changed.
    /// </summary>
    StatusChange = 1,

    /// <summary>
    /// Git branch created.
    /// </summary>
    BranchCreated = 2,

    /// <summary>
    /// Git commit made.
    /// </summary>
    Commit = 3,

    /// <summary>
    /// Git push to remote.
    /// </summary>
    Push = 4,

    /// <summary>
    /// Git pull request created.
    /// </summary>
    PullRequestCreated = 5,

    /// <summary>
    /// Git pull request merged.
    /// </summary>
    PullRequestMerged = 6,

    /// <summary>
    /// File(s) created.
    /// </summary>
    FilesCreated = 10,

    /// <summary>
    /// File(s) modified.
    /// </summary>
    FilesModified = 11,

    /// <summary>
    /// File(s) deleted.
    /// </summary>
    FilesDeleted = 12,

    /// <summary>
    /// Agent message/info.
    /// </summary>
    AgentMessage = 20,

    /// <summary>
    /// Agent error.
    /// </summary>
    AgentError = 21,

    /// <summary>
    /// Command executed.
    /// </summary>
    Command = 30,

    /// <summary>
    /// Command output.
    /// </summary>
    CommandOutput = 31,

    /// <summary>
    /// Task assignment changed.
    /// </summary>
    Assignment = 40,

    /// <summary>
    /// User comment.
    /// </summary>
    Comment = 50
}

/// <summary>
/// Log severity levels.
/// </summary>
public enum TaskLogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}
