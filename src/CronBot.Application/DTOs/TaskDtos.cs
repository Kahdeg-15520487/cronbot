using System;
using CronBot.Domain.Enums;
using TaskStatus = CronBot.Domain.Enums.TaskStatus;

namespace CronBot.Application.DTOs;

/// <summary>
/// Data transfer object for creating a new task.
/// </summary>
public record CreateTaskRequest
{
    /// <summary>
    /// Title of the task.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type of the task.
    /// </summary>
    public TaskType Type { get; init; } = TaskType.Task;

    /// <summary>
    /// Sprint ID (optional).
    /// </summary>
    public Guid? SprintId { get; init; }

    /// <summary>
    /// Story points estimate (optional).
    /// </summary>
    public int? StoryPoints { get; init; }

    /// <summary>
    /// Parent task ID for subtasks (optional).
    /// </summary>
    public Guid? ParentTaskId { get; init; }
}

/// <summary>
/// Data transfer object for updating a task.
/// </summary>
public record UpdateTaskRequest
{
    /// <summary>
    /// Title of the task.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type of the task.
    /// </summary>
    public TaskType? Type { get; init; }

    /// <summary>
    /// Status of the task.
    /// </summary>
    public TaskStatus? Status { get; init; }

    /// <summary>
    /// Sprint ID.
    /// </summary>
    public Guid? SprintId { get; init; }

    /// <summary>
    /// Story points estimate.
    /// </summary>
    public int? StoryPoints { get; init; }

    /// <summary>
    /// Assignee type (User or Agent).
    /// </summary>
    public string? AssigneeType { get; init; }

    /// <summary>
    /// Assignee ID.
    /// </summary>
    public Guid? AssigneeId { get; init; }
}

/// <summary>
/// Data transfer object for task response.
/// </summary>
public record TaskResponse
{
    /// <summary>
    /// Task ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Project ID.
    /// </summary>
    public Guid ProjectId { get; init; }

    /// <summary>
    /// Task number within the project.
    /// </summary>
    public long Number { get; init; }

    /// <summary>
    /// Title of the task.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type of the task.
    /// </summary>
    public TaskType Type { get; init; }

    /// <summary>
    /// Status of the task.
    /// </summary>
    public TaskStatus Status { get; init; }

    /// <summary>
    /// Sprint ID.
    /// </summary>
    public Guid? SprintId { get; init; }

    /// <summary>
    /// Story points estimate.
    /// </summary>
    public int? StoryPoints { get; init; }

    /// <summary>
    /// Assignee type.
    /// </summary>
    public string? AssigneeType { get; init; }

    /// <summary>
    /// Assignee ID.
    /// </summary>
    public Guid? AssigneeId { get; init; }

    /// <summary>
    /// Git branch name.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Git PR URL.
    /// </summary>
    public string? GitPrUrl { get; init; }

    /// <summary>
    /// When the task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When work started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// When the task was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// Data transfer object for task comment.
/// </summary>
public record CreateTaskCommentRequest
{
    /// <summary>
    /// Comment content.
    /// </summary>
    public required string Content { get; init; }
}

/// <summary>
/// Data transfer object for task comment response.
/// </summary>
public record TaskCommentResponse
{
    /// <summary>
    /// Comment ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Task ID.
    /// </summary>
    public Guid TaskId { get; init; }

    /// <summary>
    /// Author type.
    /// </summary>
    public required string AuthorType { get; init; }

    /// <summary>
    /// Author ID.
    /// </summary>
    public Guid AuthorId { get; init; }

    /// <summary>
    /// Comment content.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// When the comment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Request to create a pull request for a task.
/// </summary>
public record CreatePullRequestRequest
{
    /// <summary>
    /// PR title (defaults to "Task #N: Title").
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// PR body/description (defaults to task description).
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Base branch to merge into (defaults to "main").
    /// </summary>
    public string? BaseBranch { get; init; }
}

/// <summary>
/// Request to merge a pull request.
/// </summary>
public record MergePullRequestRequest
{
    /// <summary>
    /// Custom merge message (optional).
    /// </summary>
    public string? MergeMessage { get; init; }
}

/// <summary>
/// Response for pull request operations.
/// </summary>
public record PullRequestResponse
{
    /// <summary>
    /// Task ID.
    /// </summary>
    public Guid TaskId { get; init; }

    /// <summary>
    /// PR number in Gitea.
    /// </summary>
    public int PrNumber { get; init; }

    /// <summary>
    /// PR URL in Gitea.
    /// </summary>
    public string? PrUrl { get; init; }

    /// <summary>
    /// Branch name.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// PR status (open, closed, merged, none, unknown).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Whether the PR has been merged.
    /// </summary>
    public bool? Merged { get; init; }

    /// <summary>
    /// Whether the PR can be merged (no conflicts).
    /// </summary>
    public bool? Mergeable { get; init; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Request to create a task log entry.
/// </summary>
public record CreateTaskLogRequest
{
    /// <summary>
    /// Type of log entry.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Log level (debug, info, warning, error).
    /// </summary>
    public string Level { get; init; } = "info";

    /// <summary>
    /// Brief message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Detailed content.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Source (agent ID, user ID, etc).
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Git commit hash.
    /// </summary>
    public string? GitCommit { get; init; }

    /// <summary>
    /// Git branch name.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Files affected.
    /// </summary>
    public List<string>? FilesAffected { get; init; }

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Response for task log entry.
/// </summary>
public record TaskLogResponse
{
    /// <summary>
    /// Log entry ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Task ID.
    /// </summary>
    public Guid TaskId { get; init; }

    /// <summary>
    /// Type of log entry.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Log level.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Brief message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Detailed content (may contain diff, commit details, etc).
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Source of the log entry.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Git commit hash.
    /// </summary>
    public string? GitCommit { get; init; }

    /// <summary>
    /// Git branch name.
    /// </summary>
    public string? GitBranch { get; init; }

    /// <summary>
    /// Files affected.
    /// </summary>
    public List<string>? FilesAffected { get; init; }

    /// <summary>
    /// When the log entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Response for task with full activity history.
/// </summary>
public record TaskWithLogsResponse
{
    /// <summary>
    /// Basic task info.
    /// </summary>
    public required TaskResponse Task { get; init; }

    /// <summary>
    /// Activity logs.
    /// </summary>
    public required List<TaskLogResponse> Logs { get; init; }

    /// <summary>
    /// Git diff summary (latest state).
    /// </summary>
    public GitDiffSummary? DiffSummary { get; init; }
}

/// <summary>
/// Summary of git changes for a task.
/// </summary>
public record GitDiffSummary
{
    /// <summary>
    /// Branch name.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Number of commits.
    /// </summary>
    public int CommitCount { get; init; }

    /// <summary>
    /// Files added.
    /// </summary>
    public List<string> FilesAdded { get; init; } = [];

    /// <summary>
    /// Files modified.
    /// </summary>
    public List<string> FilesModified { get; init; } = [];

    /// <summary>
    /// Files deleted.
    /// </summary>
    public List<string> FilesDeleted { get; init; } = [];

    /// <summary>
    /// Latest commit hash.
    /// </summary>
    public string? LatestCommit { get; init; }

    /// <summary>
    /// Latest commit message.
    /// </summary>
    public string? LatestCommitMessage { get; init; }
}

/// <summary>
/// Response for task diff.
/// </summary>
public record TaskDiffResponse
{
    /// <summary>
    /// Task ID.
    /// </summary>
    public Guid TaskId { get; init; }

    /// <summary>
    /// Branch name.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Base branch name.
    /// </summary>
    public string? BaseBranch { get; init; }

    /// <summary>
    /// Unified diff content.
    /// </summary>
    public string? Diff { get; init; }

    /// <summary>
    /// Changed files with patches.
    /// </summary>
    public List<ChangedFileResponse> Files { get; init; } = [];

    /// <summary>
    /// Commits in the branch/PR.
    /// </summary>
    public List<CommitResponse> Commits { get; init; } = [];
}

/// <summary>
/// Response for a changed file.
/// </summary>
public record ChangedFileResponse
{
    /// <summary>
    /// File path.
    /// </summary>
    public string Filename { get; init; } = string.Empty;

    /// <summary>
    /// Status: added, modified, deleted, renamed.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Number of additions.
    /// </summary>
    public int Additions { get; init; }

    /// <summary>
    /// Number of deletions.
    /// </summary>
    public int Deletions { get; init; }

    /// <summary>
    /// Total changes.
    /// </summary>
    public int Changes { get; init; }

    /// <summary>
    /// Patch content (hunks).
    /// </summary>
    public string? Patch { get; init; }
}

/// <summary>
/// Response for a commit.
/// </summary>
public record CommitResponse
{
    /// <summary>
    /// Commit SHA.
    /// </summary>
    public string Sha { get; init; } = string.Empty;

    /// <summary>
    /// Commit message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Author name.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Created at.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Request to create a review.
/// </summary>
public record CreateReviewRequest
{
    /// <summary>
    /// Review body/comment.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Review type: approved, rejected, comment.
    /// </summary>
    public string? ReviewType { get; init; }
}

/// <summary>
/// Response for a review.
/// </summary>
public record ReviewResponse
{
    /// <summary>
    /// Review ID.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Review body.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Review state.
    /// </summary>
    public string State { get; init; } = string.Empty;

    /// <summary>
    /// Author login.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Created at.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
