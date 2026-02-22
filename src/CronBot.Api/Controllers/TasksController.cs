using CronBot.Application.DTOs;
using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using CronBot.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskEntity = CronBot.Domain.Entities.Task;
using TaskStatus = CronBot.Domain.Enums.TaskStatus;

namespace CronBot.Api.Controllers;

/// <summary>
/// Controller for managing tasks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GitService _gitService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        AppDbContext context,
        GitService gitService,
        ILogger<TasksController> logger)
    {
        _context = context;
        _gitService = gitService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all tasks for a project.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetTasks(
        [FromQuery] Guid? projectId = null,
        [FromQuery] Guid? sprintId = null,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _context.Tasks.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (sprintId.HasValue)
        {
            query = query.Where(t => t.SprintId == sprintId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Number = t.Number,
                Title = t.Title,
                Description = t.Description,
                Type = t.Type,
                Status = t.Status,
                SprintId = t.SprintId,
                StoryPoints = t.StoryPoints,
                AssigneeType = t.AssigneeType,
                AssigneeId = t.AssigneeId,
                GitBranch = t.GitBranch,
                GitPrUrl = t.GitPrUrl,
                CreatedAt = t.CreatedAt,
                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt
            })
            .ToListAsync();

        return Ok(tasks);
    }

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetTask(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        return Ok(new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Number = task.Number,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Status = task.Status,
            SprintId = task.SprintId,
            StoryPoints = task.StoryPoints,
            AssigneeType = task.AssigneeType,
            AssigneeId = task.AssigneeId,
            GitBranch = task.GitBranch,
            GitPrUrl = task.GitPrUrl,
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt
        });
    }

    /// <summary>
    /// Creates a new task in a project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskResponse>> CreateTask([FromBody] CreateTaskRequest request)
    {
        // TODO: Get project ID from context
        // For now, use the first active project
        var project = await _context.Projects
            .Include(p => p.Boards)
            .FirstOrDefaultAsync(p => p.IsActive);

        if (project == null)
        {
            return BadRequest("No active project available. Create a project first.");
        }

        var board = project.Boards.FirstOrDefault();
        if (board == null)
        {
            return BadRequest("Project has no board.");
        }

        var task = new TaskEntity
        {
            ProjectId = project.Id,
            BoardId = board.Id,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            SprintId = request.SprintId,
            StoryPoints = request.StoryPoints,
            ParentTaskId = request.ParentTaskId,
            Status = TaskStatus.Backlog
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var response = new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Number = task.Number,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Status = task.Status,
            SprintId = task.SprintId,
            StoryPoints = task.StoryPoints,
            AssigneeType = task.AssigneeType,
            AssigneeId = task.AssigneeId,
            GitBranch = task.GitBranch,
            GitPrUrl = task.GitPrUrl,
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt
        };

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, response);
    }

    /// <summary>
    /// Creates a new task in a specific project.
    /// </summary>
    [HttpPost("/api/projects/{projectId}/tasks")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> CreateTaskInProject(Guid projectId, [FromBody] CreateTaskRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Boards)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        var board = project.Boards.FirstOrDefault();
        if (board == null)
        {
            return BadRequest("Project has no board.");
        }

        var task = new TaskEntity
        {
            ProjectId = project.Id,
            BoardId = board.Id,
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            SprintId = request.SprintId,
            StoryPoints = request.StoryPoints,
            ParentTaskId = request.ParentTaskId,
            Status = TaskStatus.Backlog
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var response = new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Number = task.Number,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Status = task.Status,
            SprintId = task.SprintId,
            StoryPoints = task.StoryPoints,
            AssigneeType = task.AssigneeType,
            AssigneeId = task.AssigneeId,
            GitBranch = task.GitBranch,
            GitPrUrl = task.GitPrUrl,
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt
        };

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, response);
    }

    /// <summary>
    /// Updates a task.
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        if (request.Title != null)
            task.Title = request.Title;
        if (request.Description != null)
            task.Description = request.Description;
        if (request.Type.HasValue)
            task.Type = request.Type.Value;
        if (request.Status.HasValue)
        {
            var oldStatus = task.Status;
            task.Status = request.Status.Value;

            // Update timestamps based on status change
            if (oldStatus != TaskStatus.InProgress && request.Status.Value == TaskStatus.InProgress)
            {
                task.StartedAt = DateTimeOffset.UtcNow;
            }
            else if (request.Status.Value == TaskStatus.Done && oldStatus != TaskStatus.Done)
            {
                task.CompletedAt = DateTimeOffset.UtcNow;
            }
        }
        if (request.SprintId != task.SprintId)
            task.SprintId = request.SprintId;
        if (request.StoryPoints != task.StoryPoints)
            task.StoryPoints = request.StoryPoints;
        if (request.AssigneeType != null)
            task.AssigneeType = request.AssigneeType;
        if (request.AssigneeId != task.AssigneeId)
            task.AssigneeId = request.AssigneeId;

        await _context.SaveChangesAsync();

        return Ok(new TaskResponse
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            Number = task.Number,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Status = task.Status,
            SprintId = task.SprintId,
            StoryPoints = task.StoryPoints,
            AssigneeType = task.AssigneeType,
            AssigneeId = task.AssigneeId,
            GitBranch = task.GitBranch,
            GitPrUrl = task.GitPrUrl,
            CreatedAt = task.CreatedAt,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt
        });
    }

    /// <summary>
    /// Deletes a task.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Gets comments for a task.
    /// </summary>
    [HttpGet("{id}/comments")]
    [ProducesResponseType(typeof(IEnumerable<TaskCommentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TaskCommentResponse>>> GetTaskComments(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        var comments = await _context.TaskComments
            .Where(c => c.TaskId == id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new TaskCommentResponse
            {
                Id = c.Id,
                TaskId = c.TaskId,
                AuthorType = c.AuthorType,
                AuthorId = c.AuthorId,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    /// <summary>
    /// Adds a comment to a task.
    /// </summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(TaskCommentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskCommentResponse>> AddTaskComment(Guid id, [FromBody] CreateTaskCommentRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        // TODO: Get author info from auth context
        var comment = new TaskComment
        {
            TaskId = id,
            AuthorType = "User",
            AuthorId = Guid.Empty, // TODO: Get from auth
            Content = request.Content
        };

        _context.TaskComments.Add(comment);
        await _context.SaveChangesAsync();

        var response = new TaskCommentResponse
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorType = comment.AuthorType,
            AuthorId = comment.AuthorId,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt
        };

        return CreatedAtAction(nameof(GetTaskComments), new { id = comment.TaskId }, response);
    }

    /// <summary>
    /// Creates a pull request for a task's branch.
    /// </summary>
    [HttpPost("{id}/pull-request")]
    [ProducesResponseType(typeof(PullRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PullRequestResponse>> CreatePullRequest(
        Guid id,
        [FromBody] CreatePullRequestRequest? request = null)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (task.Project == null)
        {
            return BadRequest("Task has no associated project.");
        }

        if (task.Project.GitMode != GitMode.Internal)
        {
            return BadRequest("Project does not use internal Git. PRs can only be created for internal repositories.");
        }

        if (string.IsNullOrEmpty(task.GitBranch))
        {
            return BadRequest("Task has no Git branch. The agent needs to create a branch first.");
        }

        if (string.IsNullOrEmpty(task.Project.InternalRepoUrl))
        {
            return BadRequest("Project has no internal repository URL.");
        }

        // Extract owner and repo name from URL
        // URL format: http://gitea:3000/owner/repo-name
        var repoUri = new Uri(task.Project.InternalRepoUrl);
        var pathParts = repoUri.AbsolutePath.Trim('/').Split('/');
        if (pathParts.Length < 2)
        {
            return BadRequest("Invalid repository URL format.");
        }

        var owner = pathParts[0];
        var repoName = pathParts[1];

        // Check if PR already exists
        if (!string.IsNullOrEmpty(task.GitPrUrl))
        {
            return Ok(new PullRequestResponse
            {
                TaskId = task.Id,
                PrNumber = task.GitPrId ?? 0,
                PrUrl = task.GitPrUrl,
                Branch = task.GitBranch,
                Status = "already_exists",
                Message = "Pull request already exists for this task."
            });
        }

        // Create the PR
        var title = request?.Title ?? $"Task #{task.Number}: {task.Title}";
        var body = request?.Body ?? $"## Summary\n\n{task.Description ?? "No description provided."}\n\n## Task\n\nCloses #{task.Number}";
        var baseBranch = request?.BaseBranch ?? "main";

        var pr = await _gitService.CreatePullRequestAsync(
            owner,
            repoName,
            title,
            task.GitBranch,
            baseBranch,
            body);

        if (pr == null)
        {
            return BadRequest("Failed to create pull request. Check if the branch exists and has commits.");
        }

        // Update task with PR info
        task.GitPrId = pr.Number;
        task.GitPrUrl = pr.HtmlUrl;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created PR #{PRNumber} for task {TaskId} in {Owner}/{RepoName}",
            pr.Number, task.Id, owner, repoName);

        return Ok(new PullRequestResponse
        {
            TaskId = task.Id,
            PrNumber = pr.Number,
            PrUrl = pr.HtmlUrl,
            Branch = task.GitBranch,
            Status = pr.State,
            Message = "Pull request created successfully."
        });
    }

    /// <summary>
    /// Gets the pull request for a task.
    /// </summary>
    [HttpGet("{id}/pull-request")]
    [ProducesResponseType(typeof(PullRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PullRequestResponse>> GetPullRequest(Guid id)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (string.IsNullOrEmpty(task.GitPrUrl))
        {
            return Ok(new PullRequestResponse
            {
                TaskId = task.Id,
                PrNumber = 0,
                PrUrl = null,
                Branch = task.GitBranch,
                Status = "none",
                Message = "No pull request exists for this task."
            });
        }

        // Get fresh PR status from Gitea
        if (task.Project?.GitMode == GitMode.Internal &&
            !string.IsNullOrEmpty(task.Project.InternalRepoUrl) &&
            task.GitPrId.HasValue)
        {
            var repoUri = new Uri(task.Project.InternalRepoUrl);
            var pathParts = repoUri.AbsolutePath.Trim('/').Split('/');
            if (pathParts.Length >= 2)
            {
                var owner = pathParts[0];
                var repoName = pathParts[1];

                var pr = await _gitService.GetPullRequestAsync(owner, repoName, task.GitPrId.Value);
                if (pr != null)
                {
                    return Ok(new PullRequestResponse
                    {
                        TaskId = task.Id,
                        PrNumber = pr.Number,
                        PrUrl = pr.HtmlUrl,
                        Branch = task.GitBranch,
                        Status = pr.State,
                        Merged = pr.Merged,
                        Mergeable = pr.Mergeable,
                        Message = pr.Merged ? "Pull request has been merged." : "Pull request is open."
                    });
                }
            }
        }

        return Ok(new PullRequestResponse
        {
            TaskId = task.Id,
            PrNumber = task.GitPrId ?? 0,
            PrUrl = task.GitPrUrl,
            Branch = task.GitBranch,
            Status = "unknown",
            Message = "Could not fetch pull request status."
        });
    }

    /// <summary>
    /// Merges the pull request for a task.
    /// </summary>
    [HttpPost("{id}/pull-request/merge")]
    [ProducesResponseType(typeof(PullRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PullRequestResponse>> MergePullRequest(
        Guid id,
        [FromBody] MergePullRequestRequest? request = null)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        if (!task.GitPrId.HasValue || string.IsNullOrEmpty(task.GitPrUrl))
        {
            return BadRequest("No pull request exists for this task.");
        }

        if (task.Project?.GitMode != GitMode.Internal || string.IsNullOrEmpty(task.Project.InternalRepoUrl))
        {
            return BadRequest("Project does not use internal Git.");
        }

        var repoUri = new Uri(task.Project.InternalRepoUrl);
        var pathParts = repoUri.AbsolutePath.Trim('/').Split('/');
        if (pathParts.Length < 2)
        {
            return BadRequest("Invalid repository URL format.");
        }

        var owner = pathParts[0];
        var repoName = pathParts[1];

        var success = await _gitService.MergePullRequestAsync(
            owner,
            repoName,
            task.GitPrId.Value,
            request?.MergeMessage ?? $"Merge PR #{task.GitPrId}: {task.Title}");

        if (!success)
        {
            return BadRequest("Failed to merge pull request. It may have conflicts or already be merged.");
        }

        // Update task status
        task.Status = TaskStatus.Done;
        task.CompletedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Merged PR #{PRNumber} for task {TaskId}",
            task.GitPrId, task.Id);

        return Ok(new PullRequestResponse
        {
            TaskId = task.Id,
            PrNumber = task.GitPrId.Value,
            PrUrl = task.GitPrUrl,
            Branch = task.GitBranch,
            Status = "merged",
            Merged = true,
            Message = "Pull request merged successfully."
        });
    }

    /// <summary>
    /// Gets activity logs for a task.
    /// </summary>
    [HttpGet("{id}/logs")]
    [ProducesResponseType(typeof(IEnumerable<TaskLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TaskLogResponse>>> GetTaskLogs(
        Guid id,
        [FromQuery] string? type = null,
        [FromQuery] string? level = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound("Task not found.");
        }

        var query = _context.TaskLogs
            .Where(l => l.TaskId == id)
            .AsQueryable();

        // Filter by type if specified
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<TaskLogType>(type, true, out var logType))
        {
            query = query.Where(l => l.Type == logType);
        }

        // Filter by level if specified
        if (!string.IsNullOrEmpty(level) && Enum.TryParse<TaskLogLevel>(level, true, out var logLevel))
        {
            query = query.Where(l => l.Level == logLevel);
        }

        var logData = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
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

        var logs = logData.Select(l => new TaskLogResponse
        {
            Id = l.Id,
            TaskId = l.TaskId,
            Type = l.Type,
            Level = l.Level,
            Message = l.Message,
            Details = l.Details,
            Source = l.Source,
            GitCommit = l.GitCommit,
            GitBranch = l.GitBranch,
            FilesAffected = l.FilesAffected != null
                ? JsonSerializer.Deserialize<List<string>>(l.FilesAffected)
                : null,
            CreatedAt = l.CreatedAt
        }).ToList();

        return Ok(logs);
    }

    /// <summary>
    /// Adds a log entry to a task (for agents to report activity).
    /// </summary>
    [HttpPost("{id}/logs")]
    [ProducesResponseType(typeof(TaskLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskLogResponse>> AddTaskLog(
        Guid id,
        [FromBody] CreateTaskLogRequest request)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
        {
            return NotFound("Task not found.");
        }

        // Parse type
        if (!Enum.TryParse<TaskLogType>(request.Type, true, out var logType))
        {
            return BadRequest($"Invalid log type: {request.Type}");
        }

        // Parse level
        if (!Enum.TryParse<TaskLogLevel>(request.Level, true, out var logLevel))
        {
            logLevel = TaskLogLevel.Info;
        }

        var log = new TaskLog
        {
            TaskId = id,
            Type = logType,
            Level = logLevel,
            Message = request.Message,
            Details = request.Details,
            Source = request.Source,
            GitCommit = request.GitCommit,
            GitBranch = request.GitBranch ?? task.GitBranch,
            FilesAffected = request.FilesAffected != null
                ? JsonSerializer.Serialize(request.FilesAffected)
                : null,
            Metadata = request.Metadata != null
                ? JsonSerializer.Serialize(request.Metadata)
                : null
        };

        _context.TaskLogs.Add(log);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Added log entry {LogType} for task {TaskId}: {Message}",
            logType, id, request.Message);

        var response = new TaskLogResponse
        {
            Id = log.Id,
            TaskId = log.TaskId,
            Type = log.Type.ToString(),
            Level = log.Level.ToString(),
            Message = log.Message,
            Details = log.Details,
            Source = log.Source,
            GitCommit = log.GitCommit,
            GitBranch = log.GitBranch,
            FilesAffected = request.FilesAffected,
            CreatedAt = log.CreatedAt
        };

        return CreatedAtAction(nameof(GetTaskLogs), new { id = log.TaskId }, response);
    }

    /// <summary>
    /// Gets a task with full activity history and git diff summary.
    /// </summary>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(TaskWithLogsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskWithLogsResponse>> GetTaskHistory(Guid id)
    {
        var task = await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.Logs)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
        {
            return NotFound("Task not found.");
        }

        var logs = task.Logs
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new TaskLogResponse
            {
                Id = l.Id,
                TaskId = l.TaskId,
                Type = l.Type.ToString(),
                Level = l.Level.ToString(),
                Message = l.Message,
                Details = l.Details,
                Source = l.Source,
                GitCommit = l.GitCommit,
                GitBranch = l.GitBranch,
                FilesAffected = l.FilesAffected != null
                    ? JsonSerializer.Deserialize<List<string>>(l.FilesAffected)
                    : null,
                CreatedAt = l.CreatedAt
            })
            .ToList();

        // Build git diff summary from logs
        var gitDiffSummary = BuildGitDiffSummary(task, task.Logs.ToList());

        var response = new TaskWithLogsResponse
        {
            Task = new TaskResponse
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                Number = task.Number,
                Title = task.Title,
                Description = task.Description,
                Type = task.Type,
                Status = task.Status,
                SprintId = task.SprintId,
                StoryPoints = task.StoryPoints,
                AssigneeType = task.AssigneeType,
                AssigneeId = task.AssigneeId,
                GitBranch = task.GitBranch,
                GitPrUrl = task.GitPrUrl,
                CreatedAt = task.CreatedAt,
                StartedAt = task.StartedAt,
                CompletedAt = task.CompletedAt
            },
            Logs = logs,
            DiffSummary = gitDiffSummary
        };

        return Ok(response);
    }

    /// <summary>
    /// Build a git diff summary from task logs.
    /// </summary>
    private static GitDiffSummary? BuildGitDiffSummary(TaskEntity task, List<TaskLog> logs)
    {
        if (string.IsNullOrEmpty(task.GitBranch))
        {
            return null;
        }

        var commits = logs.Where(l => l.Type == TaskLogType.Commit).ToList();
        var filesCreated = new HashSet<string>();
        var filesModified = new HashSet<string>();
        var filesDeleted = new HashSet<string>();

        foreach (var log in logs)
        {
            if (log.FilesAffected == null) continue;

            try
            {
                var files = JsonSerializer.Deserialize<List<string>>(log.FilesAffected);
                if (files == null) continue;

                switch (log.Type)
                {
                    case TaskLogType.FilesCreated:
                        foreach (var f in files) filesCreated.Add(f);
                        break;
                    case TaskLogType.FilesModified:
                        foreach (var f in files) filesModified.Add(f);
                        break;
                    case TaskLogType.FilesDeleted:
                        foreach (var f in files) filesDeleted.Add(f);
                        break;
                }
            }
            catch
            {
                // Ignore JSON parsing errors
            }
        }

        var latestCommit = commits.FirstOrDefault();

        return new GitDiffSummary
        {
            Branch = task.GitBranch,
            CommitCount = commits.Count,
            FilesAdded = filesCreated.ToList(),
            FilesModified = filesModified.ToList(),
            FilesDeleted = filesDeleted.ToList(),
            LatestCommit = latestCommit?.GitCommit,
            LatestCommitMessage = latestCommit?.Message
        };
    }
}
