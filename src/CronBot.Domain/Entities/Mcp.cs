using CronBot.Domain.Enums;

namespace CronBot.Domain.Entities;

/// <summary>
/// Represents an MCP (Model Context Protocol) configuration.
/// </summary>
public class Mcp : AuditableEntity
{
    /// <summary>
    /// Name of the MCP.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version of the MCP.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Description of the MCP.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Group type of the MCP.
    /// </summary>
    public McpGroup GroupType { get; set; } = McpGroup.System;

    /// <summary>
    /// ID of the team (for custom MCPs).
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// ID of the project (for project MCPs).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Docker container image for the MCP.
    /// </summary>
    public string? ContainerImage { get; set; }

    /// <summary>
    /// Komodo stack ID if deployed as a stack.
    /// </summary>
    public string? KomodoStackId { get; set; }

    /// <summary>
    /// Transport protocol for the MCP.
    /// </summary>
    public McpTransport Transport { get; set; } = McpTransport.Http;

    /// <summary>
    /// Command to run (for stdio transport).
    /// </summary>
    public string? Command { get; set; }

    /// <summary>
    /// URL endpoint (for HTTP/SSE transport).
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Environment variables as JSON.
    /// </summary>
    public string EnvironmentVars { get; set; } = "{}";

    /// <summary>
    /// Required configuration schema as JSON.
    /// </summary>
    public string RequiredConfig { get; set; } = "[]";

    /// <summary>
    /// Discovered tools as JSON array.
    /// </summary>
    public string Tools { get; set; } = "[]";

    /// <summary>
    /// Discovered resources as JSON array.
    /// </summary>
    public string Resources { get; set; } = "[]";

    /// <summary>
    /// Discovered prompts as JSON array.
    /// </summary>
    public string Prompts { get; set; } = "[]";

    /// <summary>
    /// Current status of the MCP.
    /// </summary>
    public McpStatus Status { get; set; } = McpStatus.Registered;

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// When the MCP was last health checked.
    /// </summary>
    public DateTimeOffset? LastHealthCheck { get; set; }

    /// <summary>
    /// ID of the project that created this MCP (for custom MCPs).
    /// </summary>
    public Guid? CreatedByProjectId { get; set; }

    // Navigation properties
    public Team? Team { get; set; }
    public Project? Project { get; set; }
}
