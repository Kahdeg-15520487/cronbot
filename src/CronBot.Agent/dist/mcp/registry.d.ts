import { McpServerConfig, McpTool, OperationResult, AutonomyLevel } from '../types.js';
/**
 * Registry for managing MCP server connections.
 */
export declare class McpRegistry {
    private clients;
    private autonomyLevel;
    private pendingApprovals;
    constructor(autonomyLevel: AutonomyLevel);
    /**
     * Register and connect to an MCP server.
     */
    registerServer(name: string, config: McpServerConfig): Promise<OperationResult>;
    /**
     * Unregister and disconnect from an MCP server.
     */
    unregisterServer(name: string): Promise<void>;
    /**
     * Get all available tools from all servers.
     */
    getAllTools(): Array<{
        server: string;
        tool: McpTool;
    }>;
    /**
     * Find which server has a specific tool.
     */
    findToolServer(toolName: string): string | null;
    /**
     * Call a tool by name, automatically routing to the correct server.
     */
    callTool(name: string, args: Record<string, unknown>): Promise<OperationResult>;
    /**
     * Check if an operation needs approval based on autonomy level.
     */
    private checkApprovalNeeded;
    /**
     * Request approval for a pending operation.
     */
    requestApproval(toolName: string, args: Record<string, unknown>, reason: string): string;
    /**
     * Approve a pending operation.
     */
    approveOperation(approvalId: string): Promise<OperationResult>;
    /**
     * Reject a pending operation.
     */
    rejectOperation(approvalId: string): OperationResult;
    /**
     * Get all pending approvals.
     */
    getPendingApprovals(): Array<{
        id: string;
        tool: string;
        reason: string;
    }>;
    /**
     * Call a tool without approval checks (for approved operations).
     */
    private callToolUnsafe;
    /**
     * Read a resource from a specific server.
     */
    readResource(serverName: string, uri: string): Promise<OperationResult>;
    /**
     * Disconnect from all servers.
     */
    disconnectAll(): Promise<void>;
    /**
     * Get list of connected servers.
     */
    getConnectedServers(): string[];
    /**
     * Update autonomy level.
     */
    setAutonomyLevel(level: AutonomyLevel): void;
}
//# sourceMappingURL=registry.d.ts.map