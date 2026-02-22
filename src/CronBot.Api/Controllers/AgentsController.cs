using CronBot.Domain.Entities;
using CronBot.Domain.Enums;
using CronBot.Infrastructure.Data;
using CronBot.Infrastructure.Services;
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
    private readonly OrchestratorService _orchestrator;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        AppDbContext context,
        OrchestratorService orchestrator,
        ILogger<AgentsController> logger)
    {
        _context = context;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Agent response DTO.
    /// </summary>
    public record AgentResponse
    {
        public Guid Id { get; init; }
        public Guid ProjectId { get; init; }
        public string? Name { get; init; }
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
        public int AutonomyLevel { get; init; }
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
                Name = a.Name,
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
                CommitsMade = a.CommitsMade,
                AutonomyLevel = a.AutonomyLevel
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
            Name = agent.Name,
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
            CommitsMade = agent.CommitsMade,
            AutonomyLevel = agent.AutonomyLevel
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
                Name = a.Name,
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
                CommitsMade = a.CommitsMade,
                AutonomyLevel = a.AutonomyLevel
            })
            .ToListAsync();

        return Ok(agents);
    }

    /// <summary>
    /// Spawns a new agent for a project (creates Docker container).
    /// </summary>
    [HttpPost("/api/projects/{projectId}/agents")]
    [ProducesResponseType(typeof(AgentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgentResponse>> SpawnAgent(
        Guid projectId,
        [FromBody] SpawnAgentRequest? request = null)
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

        try
        {
            // Spawn agent container via Orchestrator
            var agent = await _orchestrator.SpawnAgentAsync(projectId, request?.Name);

            var response = new AgentResponse
            {
                Id = agent.Id,
                ProjectId = agent.ProjectId,
                Name = agent.Name,
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
                CommitsMade = agent.CommitsMade,
                AutonomyLevel = agent.AutonomyLevel
            };

            return CreatedAtAction(nameof(GetAgent), new { id = agent.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to spawn agent for project {ProjectId}", projectId);
            return BadRequest($"Failed to spawn agent: {ex.Message}");
        }
    }

    /// <summary>
    /// Terminates an agent (stops Docker container).
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

        try
        {
            await _orchestrator.StopAgentAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop agent container {AgentId}", id);
        }

        agent.Status = AgentStatus.Terminated;
        agent.TerminatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new AgentResponse
        {
            Id = agent.Id,
            ProjectId = agent.ProjectId,
            Name = agent.Name,
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
            CommitsMade = agent.CommitsMade,
            AutonomyLevel = agent.AutonomyLevel
        });
    }

    /// <summary>
    /// Deletes an agent (removes Docker container).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAgent(Guid id)
    {
        var agent = await _context.Agents.FindAsync(id);

        if (agent == null)
        {
            return NotFound();
        }

        try
        {
            await _orchestrator.RemoveAgentAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove agent container {AgentId}", id);
            // Still remove from database even if container removal fails
            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    /// <summary>
    /// Gets logs for an agent.
    /// </summary>
    [HttpGet("{id}/logs")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<string>> GetAgentLogs(
        Guid id,
        [FromQuery] int tail = 100)
    {
        var agent = await _context.Agents.FindAsync(id);

        if (agent == null)
        {
            return NotFound();
        }

        try
        {
            var logs = await _orchestrator.GetAgentLogsAsync(id, tail);
            return Ok(logs ?? "No logs available (container may have been removed).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get logs for agent {AgentId}", id);
            return Ok($"Error retrieving logs: {ex.Message}");
        }
    }
}

/// <summary>
/// Request to spawn a new agent.
/// </summary>
public record SpawnAgentRequest
{
    /// <summary>
    /// Optional name for the agent.
    /// </summary>
    public string? Name { get; init; }
}
