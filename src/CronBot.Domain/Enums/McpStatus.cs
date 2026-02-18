namespace CronBot.Domain.Enums;

/// <summary>
/// Deployment status of an MCP.
/// </summary>
public enum McpStatus
{
    /// <summary>
    /// Registered but not yet deployed.
    /// </summary>
    Registered = 0,

    /// <summary>
    /// Currently deploying.
    /// </summary>
    Deploying = 1,

    /// <summary>
    /// Running and healthy.
    /// </summary>
    Running = 2,

    /// <summary>
    /// Stopped, not running.
    /// </summary>
    Stopped = 3,

    /// <summary>
    /// Failed to start or crashed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Deprecated, will be removed.
    /// </summary>
    Deprecated = 5
}
