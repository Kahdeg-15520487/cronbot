using System.ComponentModel;
using System.Text.Json;
using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using TaskEntity = CronBot.Domain.Entities.Task;
using TaskStatusEnum = CronBot.Domain.Enums.TaskStatus;

namespace CronBot.Api.Mcp;

/// <summary>
/// MCP tools for Kanban operations.
/// </summary>
[McpServerToolType]
public static class KanbanTools
{
    // ========================================
    // PROJECT TOOLS
    // ========================================

    [McpServerTool, Description("List all available projects. Optionally filter by team ID.")]
    public static async Task<string> ListProjects(
        AppDbContext db,
        [Description("Optional team ID to filter projects")] Guid? teamId = null)
    {
        var query = db.Projects.Where(p => p.IsActive);

        if (teamId.HasValue)
        {
            query = query.Where(p => p.TeamId == teamId.Value);
        }

        var projects = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.TeamId,
                p.Name,
                p.Slug,
                p.Description,
                p.AutonomyLevel,
                p.MaxAgents,
                p.CreatedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(projects, JsonOptions);
    }

    [McpServerTool, Description("Get details of a specific project by ID.")]
    public static async Task<string> GetProject(
        AppDbContext db,
        [Description("The project ID")] Guid projectId)
    {
        var project = await db.Projects.FindAsync(projectId);

        if (project == null)
        {
            return JsonSerializer.Serialize(new { error = "Project not found" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            project.Id,
            project.TeamId,
            project.Name,
            project.Slug,
            project.Description,
            project.GitMode,
            project.InternalRepoUrl,
            project.AutonomyLevel,
            project.SandboxMode,
            project.MaxAgents,
            project.IsActive,
            project.CreatedAt
        }, JsonOptions);
    }

    // ========================================
    // TASK TOOLS
    // ========================================

    [McpServerTool, Description("List tasks with optional filters for project, status, or sprint.")]
    public static async Task<string> ListTasks(
        AppDbContext db,
        [Description("Optional project ID to filter tasks")] Guid? projectId = null,
        [Description("Optional sprint ID to filter tasks")] Guid? sprintId = null,
        [Description("Optional status filter: Backlog, Sprint, InProgress, Review, Blocked, Done, Cancelled")] string? status = null,
        [Description("Number of tasks to skip")] int skip = 0,
        [Description("Number of tasks to return (max 100)")] int take = 50)
    {
        var query = db.Tasks.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (sprintId.HasValue)
        {
            query = query.Where(t => t.SprintId == sprintId.Value);
        }

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskStatusEnum>(status, true, out var taskStatus))
        {
            query = query.Where(t => t.Status == taskStatus);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(Math.Min(take, 100))
            .Select(t => new
            {
                t.Id,
                t.ProjectId,
                t.Number,
                t.Title,
                t.Description,
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                t.SprintId,
                t.StoryPoints,
                AssigneeType = t.AssigneeType,
                t.AssigneeId,
                t.GitBranch,
                t.GitPrUrl,
                t.CreatedAt,
                t.StartedAt,
                t.CompletedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(tasks, JsonOptions);
    }

    [McpServerTool, Description("Get details of a specific task by ID.")]
    public static async Task<string> GetTask(
        AppDbContext db,
        [Description("The task ID")] Guid taskId)
    {
        var task = await db.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            task.Id,
            task.ProjectId,
            task.Number,
            task.Title,
            task.Description,
            Type = task.Type.ToString(),
            Status = task.Status.ToString(),
            task.SprintId,
            task.BoardId,
            task.StoryPoints,
            AssigneeType = task.AssigneeType,
            task.AssigneeId,
            task.ParentTaskId,
            task.BlockedByTaskId,
            task.BlockedReason,
            task.GitBranch,
            task.GitPrId,
            task.GitPrUrl,
            task.CreatedAt,
            task.StartedAt,
            task.CompletedAt
        }, JsonOptions);
    }

    [McpServerTool, Description("Create a new task in a project.")]
    public static async Task<string> CreateTask(
        AppDbContext db,
        [Description("The project ID to create the task in")] Guid projectId,
        [Description("Task title")] string title,
        [Description("Optional task description")] string? description = null,
        [Description("Optional task type: Task, Epic, Subtask")] string? type = null,
        [Description("Optional sprint ID")] Guid? sprintId = null,
        [Description("Optional story points estimate")] int? storyPoints = null,
        [Description("Optional parent task ID (for subtasks)")] Guid? parentTaskId = null)
    {
        var project = await db.Projects
            .Include(p => p.Boards)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);

        if (project == null)
        {
            return JsonSerializer.Serialize(new { error = "Project not found or inactive" }, JsonOptions);
        }

        var board = project.Boards.FirstOrDefault();
        if (board == null)
        {
            return JsonSerializer.Serialize(new { error = "Project has no board" }, JsonOptions);
        }

        var taskType = TaskType.Task;
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TaskType>(type, true, out var parsedType))
        {
            taskType = parsedType;
        }

        var task = new TaskEntity
        {
            ProjectId = project.Id,
            BoardId = board.Id,
            Title = title,
            Description = description,
            Type = taskType,
            SprintId = sprintId,
            StoryPoints = storyPoints,
            ParentTaskId = parentTaskId,
            Status = TaskStatusEnum.Backlog
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success = true,
            task.Id,
            task.ProjectId,
            task.Number,
            task.Title,
            Status = task.Status.ToString(),
            message = $"Task #{task.Number} created: {title}"
        }, JsonOptions);
    }

    [McpServerTool, Description("Update a task's fields.")]
    public static async Task<string> UpdateTask(
        AppDbContext db,
        [Description("The task ID to update")] Guid taskId,
        [Description("Optional new title")] string? title = null,
        [Description("Optional new description")] string? description = null,
        [Description("Optional new status: Backlog, Sprint, InProgress, Review, Blocked, Done, Cancelled")] string? status = null,
        [Description("Optional story points")] int? storyPoints = null,
        [Description("Optional sprint ID")] Guid? sprintId = null)
    {
        var task = await db.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        if (!string.IsNullOrEmpty(title))
            task.Title = title;

        if (description != null)
            task.Description = description;

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskStatusEnum>(status, true, out var newStatus))
        {
            var oldStatus = task.Status;
            task.Status = newStatus;

            // Update timestamps based on status change
            if (oldStatus != TaskStatusEnum.InProgress && newStatus == TaskStatusEnum.InProgress)
            {
                task.StartedAt = DateTimeOffset.UtcNow;
            }
            else if (newStatus == TaskStatusEnum.Done && oldStatus != TaskStatusEnum.Done)
            {
                task.CompletedAt = DateTimeOffset.UtcNow;
            }
        }

        if (storyPoints.HasValue)
            task.StoryPoints = storyPoints;

        if (sprintId != task.SprintId)
            task.SprintId = sprintId;

        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success = true,
            task.Id,
            task.Number,
            task.Title,
            Status = task.Status.ToString(),
            message = $"Task #{task.Number} updated"
        }, JsonOptions);
    }

    [McpServerTool, Description("Move a task to a different status column.")]
    public static async Task<string> MoveTask(
        AppDbContext db,
        [Description("The task ID")] Guid taskId,
        [Description("Target status: Backlog, Sprint, InProgress, Review, Blocked, Done, Cancelled")] string status)
    {
        if (!Enum.TryParse<TaskStatusEnum>(status, true, out var newStatus))
        {
            return JsonSerializer.Serialize(new { error = $"Invalid status: {status}" }, JsonOptions);
        }

        var task = await db.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        var oldStatus = task.Status;
        task.Status = newStatus;

        // Update timestamps based on status change
        if (oldStatus != TaskStatusEnum.InProgress && newStatus == TaskStatusEnum.InProgress)
        {
            task.StartedAt = DateTimeOffset.UtcNow;
        }
        else if (newStatus == TaskStatusEnum.Done && oldStatus != TaskStatusEnum.Done)
        {
            task.CompletedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success = true,
            task.Id,
            task.Number,
            task.Title,
            OldStatus = oldStatus.ToString(),
            NewStatus = newStatus.ToString(),
            message = $"Task #{task.Number} moved from {oldStatus} to {newStatus}"
        }, JsonOptions);
    }

    [McpServerTool, Description("Get the next task ready to be worked on (tasks in Sprint status).")]
    public static async Task<string> GetNextTask(
        AppDbContext db,
        [Description("Optional project ID to get next task from")] Guid? projectId = null)
    {
        var query = db.Tasks.Where(t => t.Status == TaskStatusEnum.Sprint);

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        var task = await query
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (task == null)
        {
            return JsonSerializer.Serialize(new { message = "No tasks ready for work (Sprint status)" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            task.Id,
            task.ProjectId,
            task.Number,
            task.Title,
            task.Description,
            task.StoryPoints,
            Status = task.Status.ToString()
        }, JsonOptions);
    }

    // ========================================
    // COMMENT TOOLS
    // ========================================

    [McpServerTool, Description("Add a comment to a task.")]
    public static async Task<string> AddTaskComment(
        AppDbContext db,
        [Description("The task ID")] Guid taskId,
        [Description("Comment content")] string content)
    {
        var task = await db.Tasks.FindAsync(taskId);

        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        var comment = new TaskComment
        {
            TaskId = taskId,
            AuthorType = "Agent",
            AuthorId = Guid.Empty, // TODO: Get from auth context
            Content = content
        };

        db.TaskComments.Add(comment);
        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success = true,
            comment.Id,
            comment.TaskId,
            comment.Content,
            comment.CreatedAt,
            message = "Comment added"
        }, JsonOptions);
    }

    [McpServerTool, Description("Get all comments for a task.")]
    public static async Task<string> GetTaskComments(
        AppDbContext db,
        [Description("The task ID")] Guid taskId)
    {
        var comments = await db.TaskComments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.TaskId,
                c.AuthorType,
                c.AuthorId,
                c.Content,
                c.CreatedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(comments, JsonOptions);
    }

    // ========================================
    // SPRINT TOOLS
    // ========================================

    [McpServerTool, Description("List all sprints for a project.")]
    public static async Task<string> ListSprints(
        AppDbContext db,
        [Description("The project ID")] Guid projectId)
    {
        var sprints = await db.Sprints
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new
            {
                s.Id,
                s.ProjectId,
                s.Name,
                s.Goal,
                Status = s.Status.ToString(),
                s.StartDate,
                s.EndDate,
                s.VelocityPoints,
                s.CreatedAt
            })
            .ToListAsync();

        return JsonSerializer.Serialize(sprints, JsonOptions);
    }

    [McpServerTool, Description("Get the currently active sprint for a project.")]
    public static async Task<string> GetActiveSprint(
        AppDbContext db,
        [Description("The project ID")] Guid projectId)
    {
        var sprint = await db.Sprints
            .Where(s => s.ProjectId == projectId && s.Status == SprintStatus.Active)
            .FirstOrDefaultAsync();

        if (sprint == null)
        {
            return JsonSerializer.Serialize(new { message = "No active sprint" }, JsonOptions);
        }

        return JsonSerializer.Serialize(new
        {
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            Status = sprint.Status.ToString(),
            sprint.StartDate,
            sprint.EndDate,
            sprint.VelocityPoints
        }, JsonOptions);
    }

    // ========================================
    // BOARD TOOLS
    // ========================================

    [McpServerTool, Description("Get the Kanban board for a project with tasks organized by status.")]
    public static async Task<string> GetBoard(
        AppDbContext db,
        [Description("The project ID")] Guid projectId)
    {
        var board = await db.Boards
            .FirstOrDefaultAsync(b => b.ProjectId == projectId);

        if (board == null)
        {
            return JsonSerializer.Serialize(new { error = "Board not found" }, JsonOptions);
        }

        var tasks = await db.Tasks
            .Where(t => t.ProjectId == projectId)
            .Select(t => new
            {
                t.Id,
                t.Number,
                t.Title,
                Status = t.Status.ToString(),
                t.StoryPoints
            })
            .ToListAsync();

        // Group tasks by status
        var columns = new Dictionary<string, List<object>>();
        foreach (var status in Enum.GetNames(typeof(TaskStatusEnum)))
        {
            columns[status] = new List<object>();
        }

        foreach (var task in tasks)
        {
            if (columns.TryGetValue(task.Status, out var column))
            {
                column.Add(task);
            }
        }

        return JsonSerializer.Serialize(new
        {
            board.Id,
            board.Name,
            Columns = board.Columns,
            TasksByStatus = columns
        }, JsonOptions);
    }

    // ========================================
    // TASK LOGGING TOOLS
    // ========================================

    [McpServerTool, Description("Log activity for a task. Use this to record commits, file changes, and other work done on a task.")]
    public static async Task<string> LogTaskActivity(
        AppDbContext db,
        [Description("The task ID")] Guid taskId,
        [Description("Activity type: StatusChange, BranchCreated, Commit, Push, PullRequestCreated, PullRequestMerged, FilesCreated, FilesModified, FilesDeleted, AgentMessage, AgentError, Command, CommandOutput")] string type,
        [Description("Activity message")] string message,
        [Description("Optional detailed content (can include diff, command output, etc.)")] string? details = null,
        [Description("Optional log level: debug, info, warning, error")] string level = "info",
        [Description("Optional source (e.g., agent ID)")] string? source = null,
        [Description("Optional git commit hash")] string? gitCommit = null,
        [Description("Optional git branch name")] string? gitBranch = null,
        [Description("Optional JSON array of affected files")] string? filesAffected = null)
    {
        var task = await db.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        if (!Enum.TryParse<TaskLogType>(type, true, out var logType))
        {
            return JsonSerializer.Serialize(new { error = $"Invalid log type: {type}. Valid types: StatusChange, BranchCreated, Commit, Push, PullRequestCreated, PullRequestMerged, FilesCreated, FilesModified, FilesDeleted, AgentMessage, AgentError, Command, CommandOutput" }, JsonOptions);
        }

        if (!Enum.TryParse<TaskLogLevel>(level, true, out var logLevel))
        {
            logLevel = TaskLogLevel.Info;
        }

        var log = new TaskLog
        {
            TaskId = taskId,
            Type = logType,
            Level = logLevel,
            Message = message,
            Details = details,
            Source = source,
            GitCommit = gitCommit,
            GitBranch = gitBranch ?? task.GitBranch,
            FilesAffected = filesAffected
        };

        db.TaskLogs.Add(log);
        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success = true,
            log.Id,
            log.TaskId,
            Type = log.Type.ToString(),
            Level = log.Level.ToString(),
            log.Message,
            log.GitCommit,
            log.GitBranch,
            log.CreatedAt,
            message = $"Activity logged: {type}"
        }, JsonOptions);
    }

    [McpServerTool, Description("Get activity history for a task, including commits, file changes, and agent messages.")]
    public static async Task<string> GetTaskHistory(
        AppDbContext db,
        [Description("The task ID")] Guid taskId,
        [Description("Optional filter by log type")] string? type = null,
        [Description("Number of logs to skip")] int skip = 0,
        [Description("Number of logs to return (max 100)")] int take = 50)
    {
        var task = await db.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        var query = db.TaskLogs.Where(l => l.TaskId == taskId);

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TaskLogType>(type, true, out var logType))
        {
            query = query.Where(l => l.Type == logType);
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(Math.Min(take, 100))
            .Select(l => new
            {
                l.Id,
                l.TaskId,
                Type = l.Type.ToString(),
                Level = l.Level.ToString(),
                l.Message,
                l.Details,
                l.Source,
                l.GitCommit,
                l.GitBranch,
                l.FilesAffected,
                l.CreatedAt
            })
            .ToListAsync();

        // Build git diff summary
        var commitCount = logs.Count(l => l.Type == nameof(TaskLogType.Commit));
        var latestCommit = logs.FirstOrDefault(l => l.Type == nameof(TaskLogType.Commit));

        var summary = new
        {
            TaskId = taskId,
            TaskNumber = task.Number,
            TaskTitle = task.Title,
            TaskStatus = task.Status.ToString(),
            GitBranch = task.GitBranch,
            GitPrUrl = task.GitPrUrl,
            CommitCount = commitCount,
            LatestCommit = latestCommit != null ? new
            {
                latestCommit.GitCommit,
                latestCommit.Message
            } : null,
            Logs = logs
        };

        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    [McpServerTool, Description("Update the git branch name for a task. Call this when creating a new branch.")]
    public static async Task<string> SetTaskBranch(
        AppDbContext db,
        [Description("The task ID")] Guid taskId,
        [Description("The git branch name")] string branch)
    {
        var task = await db.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return JsonSerializer.Serialize(new { error = "Task not found" }, JsonOptions);
        }

        var oldBranch = task.GitBranch;
        task.GitBranch = branch;

        // Log the branch creation
        var log = new TaskLog
        {
            TaskId = taskId,
            Type = TaskLogType.BranchCreated,
            Level = TaskLogLevel.Info,
            Message = $"Created branch: {branch}",
            GitBranch = branch
        };

        db.TaskLogs.Add(log);
        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            success = true,
            task.Id,
            task.Number,
            OldBranch = oldBranch,
            NewBranch = branch,
            message = $"Branch set to: {branch}"
        }, JsonOptions);
    }

    // ========================================
    // HELPER
    // ========================================

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}
