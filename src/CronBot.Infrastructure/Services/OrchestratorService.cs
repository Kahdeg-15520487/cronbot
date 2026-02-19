using CronBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Agent = CronBot.Domain.Entities.Agent;
using Project = CronBot.Domain.Entities.Project;

namespace CronBot.Infrastructure.Services;

/// <summary>
/// Service for orchestrating agent containers via Docker API.
/// </summary>
public class OrchestratorService
{
    private readonly AppDbContext _context;
    private readonly DockerClientService _docker;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrchestratorService> _logger;

    public OrchestratorService(
        AppDbContext context,
        DockerClientService docker,
        IConfiguration configuration,
        ILogger<OrchestratorService> logger)
    {
        _context = context;
        _docker = docker;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Spawn a new agent for a project.
    /// </summary>
    public async Task<Agent> SpawnAgentAsync(
        Guid projectId,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        // Get project
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        // Create agent record
        var agent = new Agent
        {
            ProjectId = projectId,
            Name = name ?? $"Agent-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
            Status = Domain.Enums.AgentStatus.Idle,
            AutonomyLevel = project.AutonomyLevel,
            ContainerId = string.Empty,
            ContainerName = string.Empty,
            Settings = "{}"
        };

        _context.Agents.Add(agent);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // Ensure network exists
            await _docker.EnsureNetworkExistsAsync(cancellationToken);

            // Create volumes for the agent
            await _docker.CreateAgentVolumesAsync(agent.Id, cancellationToken);

            // Get configuration
            var apiKey = _configuration["Anthropic:ApiKey"]
                ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not configured");
            var baseUrl = _configuration["Anthropic:BaseUrl"] ?? "";
            var kanbanUrl = _configuration["Services:KanbanUrl"] ?? "http://api:8080/api";

            // Create container name
            var containerName = $"cronbot-agent-{agent.Id.ToString()[..8]}";

            // Create and start container
            var containerId = await _docker.CreateAgentContainerAsync(
                containerName,
                agent.Id,
                projectId,
                project.AutonomyLevel,
                apiKey,
                baseUrl,
                kanbanUrl,
                cancellationToken
            );

            if (!string.IsNullOrEmpty(containerId))
            {
                // Update agent with container info
                agent.ContainerId = containerId;
                agent.ContainerName = containerName;
                agent.Status = Domain.Enums.AgentStatus.Idle;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Spawned agent {AgentId} for project {ProjectId} in container {ContainerName}",
                    agent.Id, projectId, containerName);
            }

            return agent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to spawn agent container for project {ProjectId}", projectId);
            agent.Status = Domain.Enums.AgentStatus.Error;
            agent.StatusMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Stop an agent.
    /// </summary>
    public async Task StopAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || string.IsNullOrEmpty(agent.ContainerId))
        {
            return;
        }

        try
        {
            await _docker.StopContainerAsync(agent.ContainerId, cancellationToken);

            agent.Status = Domain.Enums.AgentStatus.Terminated;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Stopped agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop agent {AgentId}", agentId);
            throw;
        }
    }

    /// <summary>
    /// Start a stopped agent.
    /// </summary>
    public async Task StartAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || string.IsNullOrEmpty(agent.ContainerId))
        {
            return;
        }

        try
        {
            await _docker.StartContainerAsync(agent.ContainerId, cancellationToken);

            agent.Status = Domain.Enums.AgentStatus.Idle;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Started agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start agent {AgentId}", agentId);
            throw;
        }
    }

    /// <summary>
    /// Remove an agent.
    /// </summary>
    public async Task RemoveAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null)
        {
            return;
        }

        // Stop first if running
        if (agent.Status != Domain.Enums.AgentStatus.Terminated)
        {
            await StopAgentAsync(agentId, cancellationToken);
        }

        try
        {
            if (!string.IsNullOrEmpty(agent.ContainerId))
            {
                await _docker.RemoveContainerAsync(agent.ContainerId, force: true, cancellationToken);
            }

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed agent {AgentId}", agentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove agent {AgentId}", agentId);
            throw;
        }
    }

    /// <summary>
    /// Get agent status from Docker.
    /// </summary>
    public async Task<Agent?> GetAgentStatusAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || string.IsNullOrEmpty(agent.ContainerId))
        {
            return agent;
        }

        try
        {
            var container = await _docker.GetContainerAsync(agent.ContainerId, cancellationToken);

            if (container != null)
            {
                var isRunning = container.State?.Status == "running";
                agent.Status = isRunning
                    ? Domain.Enums.AgentStatus.Working
                    : Domain.Enums.AgentStatus.Terminated;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get status for agent {AgentId}", agentId);
        }

        return agent;
    }

    /// <summary>
    /// Get agent logs from Docker.
    /// </summary>
    public async Task<string?> GetAgentLogsAsync(Guid agentId, int tail = 100, CancellationToken cancellationToken = default)
    {
        var agent = await _context.Agents.FindAsync(agentId);
        if (agent == null || string.IsNullOrEmpty(agent.ContainerId))
        {
            return null;
        }

        return await _docker.GetContainerLogsAsync(agent.ContainerId, tail, cancellationToken);
    }

    /// <summary>
    /// List all agents for a project.
    /// </summary>
    public async Task<List<Agent>> ListProjectAgentsAsync(Guid projectId)
    {
        return await _context.Agents
            .Where(a => a.ProjectId == projectId)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();
    }
}
