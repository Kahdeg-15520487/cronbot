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
