using CronBot.Application.DTOs;
using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CronBot.Api.Controllers;

/// <summary>
/// Controller for managing teams.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TeamsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all teams.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TeamResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TeamResponse>>> GetTeams(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var teams = await _context.Teams
            .OrderBy(t => t.Name)
            .Skip(skip)
            .Take(take)
            .Select(t => new TeamResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description,
                IsPersonal = t.IsPersonal,
                OwnerId = t.OwnerId,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Ok(teams);
    }

    /// <summary>
    /// Gets a team by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> GetTeam(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound();
        }

        return Ok(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            Description = team.Description,
            IsPersonal = team.IsPersonal,
            OwnerId = team.OwnerId,
            CreatedAt = team.CreatedAt
        });
    }

    /// <summary>
    /// Creates a new team.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TeamResponse>> CreateTeam([FromBody] CreateTeamRequest request)
    {
        // TODO: Get owner ID from auth context
        // For now, use the first active user as owner
        var owner = await _context.Users.FirstOrDefaultAsync(u => u.IsActive);
        if (owner == null)
        {
            return BadRequest("No active user available to be team owner. Create a user first.");
        }

        var team = new Team
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            IsPersonal = request.IsPersonal,
            OwnerId = owner.Id,
            Settings = "{}"
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        var response = new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            Description = team.Description,
            IsPersonal = team.IsPersonal,
            OwnerId = team.OwnerId,
            CreatedAt = team.CreatedAt
        };

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, response);
    }

    /// <summary>
    /// Updates a team.
    /// </summary>
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(TeamResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> UpdateTeam(Guid id, [FromBody] UpdateTeamRequest request)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound();
        }

        if (request.Name != null)
            team.Name = request.Name;
        if (request.Description != null)
            team.Description = request.Description;

        await _context.SaveChangesAsync();

        return Ok(new TeamResponse
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            Description = team.Description,
            IsPersonal = team.IsPersonal,
            OwnerId = team.OwnerId,
            CreatedAt = team.CreatedAt
        });
    }

    /// <summary>
    /// Deletes a team.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTeam(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound();
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Gets all projects for a team.
    /// </summary>
    [HttpGet("{id}/projects")]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetTeamProjects(Guid id)
    {
        var team = await _context.Teams.FindAsync(id);

        if (team == null)
        {
            return NotFound();
        }

        var projects = await _context.Projects
            .Where(p => p.TeamId == id && p.IsActive)
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
}
