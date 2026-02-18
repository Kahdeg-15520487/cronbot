using CronBot.Application.DTOs;
using CronBot.Domain.Entities;
using CronBot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CronBot.Api.Controllers;

/// <summary>
/// Controller for managing projects.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProjectsController(AppDbContext context)
    {
        _context = context;
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

        var project = new Project
        {
            TeamId = team.Id,
            Name = request.Name,
            Slug = request.Slug,
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
}
