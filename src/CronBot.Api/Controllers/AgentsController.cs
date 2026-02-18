using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CronBot.Api.Controllers;

/// <summary>
/// Controller for managing agents.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AgentsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Agent response DTO.
    /// </summary>
    public record AgentResponse
    {
        public Guid Id { get; init; }
        public Guid ProjectId { get; init; }
        public Guid? CurrentTaskId { get; init; }
        public string? ContainerId { get; init; }
        public string? ContainerName { get; init; }
        public AgentStatus Status { get; init; }
        public string? StatusMessage { get; init; }
        public decimal? CpuUsagePercent { get; init; }
        public int? MemoryUsageMb { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? LastActivityAt { get; init; }
        public int TasksCompleted { get; init; }
        public int CommitsMade { get; init; }
    }

    /// <summary>
    /// Gets all agents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AgentResponse>>> GetAgents(
        [FromQuery] Guid? projectId = null,
        [FromQuery] AgentStatus? status = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _context.Agents.AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(a => a.ProjectId == projectId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var agents = await query
            .OrderByDescending(a => a.StartedAt)
            .Skip(skip)
            .Take(take)
            .Select(a => new AgentResponse
            {
                Id = a.Id,
                ProjectId = a.ProjectId,
                CurrentTaskId = a.CurrentTaskId,
                ContainerId = a.ContainerId,
                ContainerName = a.ContainerName,
                Status = a.Status,
                StatusMessage = a.StatusMessage,
                CpuUsagePercent = a.CpuUsagePercent,
                MemoryUsageMb = a.MemoryUsageMb,
                StartedAt = a.StartedAt,
                LastActivityAt = a.LastActivityAt,
                TasksCompleted = a.TasksCompleted,
                CommitsMade = a.CommitsMade
            })
            .ToListAsync();

        return Ok(agents);
    }

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentResponse>> GetAgent(Guid id)
    {
        var agent = await _context.Agents.FindAsync(id);

        if (agent == null)
        {
            return NotFound();
        }

        return Ok(new AgentResponse
        {
            Id = agent.Id,
            ProjectId = agent.ProjectId,
            CurrentTaskId = agent.CurrentTaskId,
            ContainerId = agent.ContainerId,
            ContainerName = agent.ContainerName,
            Status = agent.Status,
            StatusMessage = agent.StatusMessage,
            CpuUsagePercent = agent.CpuUsagePercent,
            MemoryUsageMb = agent.MemoryUsageMb,
            StartedAt = agent.StartedAt,
            LastActivityAt = agent.LastActivityAt,
            TasksCompleted = agent.TasksCompleted,
            CommitsMade = agent.CommitsMade
        });
    }

    /// <summary>
    /// Gets agents for a specific project.
    /// </summary>
    [HttpGet("/api/projects/{projectId}/agents")]
    [ProducesResponseType(typeof(IEnumerable<AgentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<AgentResponse>>> GetProjectAgents(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        var agents = await _context.Agents
            .Where(a => a.ProjectId == projectId)
            .Select(a => new AgentResponse
            {
                Id = a.Id,
                ProjectId = a.ProjectId,
                CurrentTaskId = a.CurrentTaskId,
                ContainerId = a.ContainerId,
                ContainerName = a.ContainerName,
                Status = a.Status,
                StatusMessage = a.StatusMessage,
                CpuUsagePercent = a.CpuUsagePercent,
                MemoryUsageMb = a.MemoryUsageMb,
                StartedAt = a.StartedAt,
                LastActivityAt = a.LastActivityAt,
                TasksCompleted = a.TasksCompleted,
                CommitsMade = a.CommitsMade
            })
            .ToListAsync();

        return Ok(agents);
    }

    /// <summary>
    /// Creates a new agent for a project.
    /// </summary>
    [HttpPost("/api/projects/{projectId}/agents")]
    [ProducesResponseType(typeof(AgentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgentResponse>> CreateAgent(Guid projectId)
    {
        var project = await _context.Projects.FindAsync(projectId);

        if (project == null)
        {
            return NotFound("Project not found.");
        }

        // Check max agents limit
        var currentAgentCount = await _context.Agents
            .CountAsync(a => a.ProjectId == projectId && a.Status != AgentStatus.Terminated);

        if (currentAgentCount >= project.MaxAgents)
        {
            return BadRequest($"Maximum number of agents ({project.MaxAgents}) reached for this project.");
        }

        var agent = new Agent
        {
            ProjectId = projectId,
            Status = AgentStatus.Idle
        };

        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        var response = new AgentResponse
        {
            Id = agent.Id,
            ProjectId = agent.ProjectId,
            CurrentTaskId = agent.CurrentTaskId,
            ContainerId = agent.ContainerId,
            ContainerName = agent.ContainerName,
            Status = agent.Status,
            StatusMessage = agent.StatusMessage,
            CpuUsagePercent = agent.CpuUsagePercent,
            MemoryUsageMb = agent.MemoryUsageMb,
            StartedAt = agent.StartedAt,
            LastActivityAt = agent.LastActivityAt,
            TasksCompleted = agent.TasksCompleted,
            CommitsMade = agent.CommitsMade
        };

        return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, response);
    }

    /// <summary>
    /// Terminates an agent.
    /// </summary>
    [HttpPost("{id}/terminate")]
    [ProducesResponseType(typeof(AgentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentResponse>> TerminateAgent(Guid id)
    {
        var agent = await _context.Agents.FindAsync(id);

        if (agent == null)
        {
            return NotFound();
        }

        agent.Status = AgentStatus.Terminated;
        agent.TerminatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new AgentResponse
        {
            Id = agent.Id,
            ProjectId = agent.ProjectId,
            CurrentTaskId = agent.CurrentTaskId,
            ContainerId = agent.ContainerId,
            ContainerName = agent.ContainerName,
            Status = agent.Status,
            StatusMessage = agent.StatusMessage,
            CpuUsagePercent = agent.CpuUsagePercent,
            MemoryUsageMb = agent.MemoryUsageMb,
            StartedAt = agent.StartedAt,
            LastActivityAt = agent.LastActivityAt,
            TasksCompleted = agent.TasksCompleted,
            CommitsMade = agent.CommitsMade
        });
    }
}
