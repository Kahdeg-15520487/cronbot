namespace CronBot.Domain.Enums;

/// <summary>
/// Status of an agent container.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is idle and available for tasks.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Agent is actively working on a task.
    /// </summary>
    Working = 1,

    /// <summary>
    /// Agent is paused, awaiting instruction.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// Agent is blocked and cannot proceed.
    /// </summary>
    Blocked = 3,

    /// <summary>
    /// Agent encountered an error.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Agent has been terminated.
    /// </summary>
    Terminated = 5
}
