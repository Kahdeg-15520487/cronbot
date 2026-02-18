using CronBot.Application.DTOs;
using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public TasksController(AppDbContext context)
    {
        _context = context;
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
}
