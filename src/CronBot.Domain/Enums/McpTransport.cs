namespace CronBot.Domain.Enums;

/// <summary>
/// Transport protocol for MCP communication.
/// </summary>
public enum McpTransport
{
    /// <summary>
    /// Standard input/output transport.
    /// </summary>
    Stdio = 0,

    /// <summary>
    /// Server-Sent Events transport.
    /// </summary>
    Sse = 1,

    /// <summary>
    /// HTTP transport.
    /// </summary>
    Http = 2
}
