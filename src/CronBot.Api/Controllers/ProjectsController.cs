using CronBot.Application.DTOs;
using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using CronBot.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskEntity = CronBot.Domain.Entities.Task;
using TaskStatus = CronBot.Domain.Enums.TaskStatus;

namespace CronBot.Api.Controllers;

/// <summary>
/// Controller for managing projects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly GitService _gitService;
    private readonly OrchestratorService _orchestrator;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        AppDbContext context,
        GitService gitService,
        OrchestratorService orchestrator,
        ILogger<ProjectsController> logger)
    {
        _context = context;
        _gitService = gitService;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects(
        [FromQuery] Guid? teamId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _context.Projects.Where(p => p.IsActive);

        if (teamId.HasValue)
        {
            query = query.Where(p => p.TeamId == teamId.Value);
        }

        var projects = await query
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                TeamId = p.TeamId,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                GitMode = p.GitMode,
                InternalRepoUrl = p.InternalRepoUrl,
                AutonomyLevel = p.AutonomyLevel,
                SandboxMode = p.SandboxMode,
                MaxAgents = p.MaxAgents,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(projects);
    }

    /// <summary>
    /// Gets a project by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> GetProject(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        return Ok(new ProjectResponse
        {
            Id = project.Id,
            TeamId = project.TeamId,
            Name = project.Name,
            Slug = project.Slug,
            Description = project.Description,
            GitMode = project.GitMode,
            InternalRepoUrl = project.InternalRepoUrl,
            AutonomyLevel = project.AutonomyLevel,
            SandboxMode = project.SandboxMode,
            MaxAgents = project.MaxAgents,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt
        });
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectResponse>> CreateProject([FromBody] CreateProjectRequest request)
    {
        // TODO: Get team ID from context/auth
        // For now, require team ID in request body or use first team
        var team = await _context.Teams.FirstOrDefaultAsync();
        if (team == null)
        {
            return BadRequest("No team available. Create a team first.");
        }

        // Generate a unique slug
        var baseSlug = request.Slug ?? GenerateSlug(request.Name);
        var slug = baseSlug;
        var counter = 1;

        while (await _context.Projects.AnyAsync(p => p.TeamId == team.Id && p.Slug == slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        var project = new Project
        {
            TeamId = team.Id,
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            GitMode = request.GitMode,
            ExternalGitUrl = request.ExternalGitUrl,
            AutonomyLevel = request.AutonomyLevel,
            SandboxMode = request.SandboxMode,
            TemplateName = request.TemplateName,
            MaxAgents = request.MaxAgents,
            Settings = "{}",
            ScalingConfig = "{}",
            ErrorHandlingConfig = "{}"
        };

        _context.Projects.Add(project);

        // Create default board for the project
        var board = new Board
        {
            ProjectId = project.Id,
            Name = "Default Board",
            Columns = "[\"Backlog\", \"Sprint\", \"In Progress\", \"Review\", \"Blocked\", \"Done\"]"
        };
        _context.Boards.Add(board);

        await _context.SaveChangesAsync();

        // Create Gitea repository for Internal GitMode
        if (project.GitMode == GitMode.Internal)
        {
            try
            {
                var repoName = $"{project.Slug}-{project.Id.ToString()[..8]}";
                var giteaRepo = await _gitService.CreateRepoAsync(
                    repoName,
                    project.Description ?? $"CronBot project: {project.Name}");

                if (giteaRepo != null)
                {
                    project.InternalRepoId = (int)giteaRepo.Id;
                    project.InternalRepoUrl = _gitService.GetWebUrl(giteaRepo.Owner?.Login ?? "cronbot", giteaRepo.Name);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Created Gitea repo {RepoName} for project {ProjectId}",
                        repoName, project.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to create Gitea repo for project {ProjectId}, continuing without repo",
                        project.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Gitea repo for project {ProjectId}",
                    project.Id);
                // Continue without Gitea repo - project is still created
            }
        }

        // Auto-create initial task to populate kanban board
        var initialTask = new TaskEntity
        {
            ProjectId = project.Id,
            BoardId = board.Id,
            Number = 1,
            Title = "Set up project tasks in kanban board's backlog",
            Description = "Initialize the project with beginning tasks and ideas. Review project requirements create tasks regarding the implementation plan.",
            Type = TaskType.Task,
            Status = TaskStatus.Sprint, // Put in Sprint so agent picks it up
            AssigneeType = "Agent"
        };
        _context.Tasks.Add(initialTask);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created initial task for project {ProjectId}",
            project.Id);

        // Auto-spawn an agent for the project
        try
        {
            var agent = await _orchestrator.SpawnAgentAsync(project.Id, $"{project.Name}-Agent-1");
            _logger.LogInformation(
                "Auto-spawned agent {AgentId} for project {ProjectId}",
                agent.Id, project.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to auto-spawn agent for project {ProjectId}",
                project.Id);
            // Continue without agent - project is still created
        }

        var response = new ProjectResponse
        {
            Id = project.Id,
            TeamId = project.TeamId,
            Name = project.Name,
            Slug = project.Slug,
            Description = project.Description,
            GitMode = project.GitMode,
            InternalRepoUrl = project.InternalRepoUrl,
            AutonomyLevel = project.AutonomyLevel,
            SandboxMode = project.SandboxMode,
            MaxAgents = project.MaxAgents,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt
        };

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, response);
    }

    /// <summary>
    /// Updates a project.
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponse>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var project = await _context.Projects.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        if (request.Name != null)
            project.Name = request.Name;
        if (request.Description != null)
            project.Description = request.Description;
        if (request.AutonomyLevel.HasValue)
            project.AutonomyLevel = request.AutonomyLevel.Value;
        if (request.SandboxMode.HasValue)
            project.SandboxMode = request.SandboxMode.Value;
        if (request.MaxAgents.HasValue)
            project.MaxAgents = request.MaxAgents.Value;
        if (request.IsActive.HasValue)
            project.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(new ProjectResponse
        {
            Id = project.Id,
            TeamId = project.TeamId,
            Name = project.Name,
            Slug = project.Slug,
            Description = project.Description,
            GitMode = project.GitMode,
            InternalRepoUrl = project.InternalRepoUrl,
            AutonomyLevel = project.AutonomyLevel,
            SandboxMode = project.SandboxMode,
            MaxAgents = project.MaxAgents,
            IsActive = project.IsActive,
            CreatedAt = project.CreatedAt
        });
    }

    /// <summary>
    /// Deletes a project (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _context.Projects.FindAsync(id);

        if (project == null)
        {
            return NotFound();
        }

        project.IsActive = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Gets the next task ready to be worked on (Sprint status).
    /// </summary>
    [HttpGet("{id:guid}/tasks/next")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse?>> GetNextTask(Guid id)
    {
        // Get the next task in Sprint status (ready to be picked up)
        var task = await _context.Tasks
            .Where(t => t.ProjectId == id && t.Status == TaskStatus.Sprint)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync();

        if (task == null)
        {
            return Ok((TaskResponse?)null);
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
    /// Gets all tasks for a project.
    /// </summary>
    [HttpGet("{id:guid}/tasks")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TaskResponse>>> GetProjectTasks(
        Guid id,
        [FromQuery] TaskStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var project = await _context.Projects.FindAsync(id);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        var query = _context.Tasks.Where(t => t.ProjectId == id);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        var tasks = await query
            .OrderBy(t => t.Number)
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
    /// Generates a URL-friendly slug from a name.
    /// </summary>
    private static string GenerateSlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "project";

        // Convert to lowercase and replace spaces with hyphens
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove invalid characters (keep only alphanumeric and hyphens)
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // Remove consecutive hyphens
        while (slug.Contains("--"))
        {
            slug = slug.Replace("--", "-");
        }

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        // Ensure slug is not empty
        if (string.IsNullOrEmpty(slug))
            slug = "project";

        return slug;
    }
}
