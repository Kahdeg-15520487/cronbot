namespace CronBot.Domain.Enums;

/// <summary>
/// Group type for MCP (Model Context Protocol) configurations.
/// </summary>
public enum McpGroup
{
    /// <summary>
    /// System-provided MCPs (filesystem, git, kanban, etc.).
    /// </summary>
    System = 0,

    /// <summary>
    /// Auto-provisioned per-project MCPs.
    /// </summary>
    Project = 1,

    /// <summary>
    /// User/agent-created custom MCPs.
    /// </summary>
    Custom = 2,

    /// <summary>
    /// Third-party external MCPs.
    /// </summary>
    External = 3
}
