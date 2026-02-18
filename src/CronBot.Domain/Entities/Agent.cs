using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents an agent container working on a project.
/// </summary>
public class Agent : Entity
{
    /// <summary>
    /// ID of the project this agent belongs to.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// ID of the current task being worked on (optional).
    /// </summary>
    public Guid? CurrentTaskId { get; set; }

    /// <summary>
    /// Docker container ID.
    /// </summary>
    public string? ContainerId { get; set; }

    /// <summary>
    /// Docker container name.
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// Current status of the agent.
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Idle;

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Current CPU usage percentage.
    /// </summary>
    public decimal? CpuUsagePercent { get; set; }

    /// <summary>
    /// Current memory usage in MB.
    /// </summary>
    public int? MemoryUsageMb { get; set; }

    /// <summary>
    /// When the agent was started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the agent was last active.
    /// </summary>
    public DateTimeOffset? LastActivityAt { get; set; }

    /// <summary>
    /// When the agent was terminated.
    /// </summary>
    public DateTimeOffset? TerminatedAt { get; set; }

    /// <summary>
    /// Number of tasks completed by this agent.
    /// </summary>
    public int TasksCompleted { get; set; }

    /// <summary>
    /// Number of commits made by this agent.
    /// </summary>
    public int CommitsMade { get; set; }

    // Navigation properties
    public Project Project { get; set; } = null!;
    public Task? CurrentTask { get; set; }
}
