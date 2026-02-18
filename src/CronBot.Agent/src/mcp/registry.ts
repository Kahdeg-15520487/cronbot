import { McpClient } from './client.js';
import { McpServerConfig, McpTool, OperationResult, AutonomyLevel } from '../types.js';
import { createLogger } from '../logger.js';

const logger = createLogger('mcp-registry');

/**
 * Registry for managing MCP server connections.
 */
export class McpRegistry {
  private clients: Map<string, McpClient> = new Map();
  private autonomyLevel: AutonomyLevel;
  private pendingApprovals: Map<string, { tool: string; args: Record<string, unknown>; reason: string }> = new Map();

  constructor(autonomyLevel: AutonomyLevel) {
    this.autonomyLevel = autonomyLevel;
  }

  /**
   * Register and connect to an MCP server.
   */
  async registerServer(name: string, config: McpServerConfig): Promise<OperationResult> {
    if (this.clients.has(name)) {
      logger.warn({ server: name }, 'Server already registered');
      return { success: true };
    }

    const client = new McpClient(config);
    const result = await client.connect();

    if (result.success) {
      this.clients.set(name, client);
      logger.info({ server: name }, 'MCP server registered');
    }

    return result;
  }

  /**
   * Unregister and disconnect from an MCP server.
   */
  async unregisterServer(name: string): Promise<void> {
    const client = this.clients.get(name);
    if (client) {
      await client.disconnect();
      this.clients.delete(name);
      logger.info({ server: name }, 'MCP server unregistered');
    }
  }

  /**
   * Get all available tools from all servers.
   */
  getAllTools(): Array<{ server: string; tool: McpTool }> {
    const tools: Array<{ server: string; tool: McpTool }> = [];

    for (const [serverName, client] of this.clients) {
      for (const tool of client.getTools()) {
        tools.push({ server: serverName, tool });
      }
    }

    return tools;
  }

  /**
   * Find which server has a specific tool.
   */
  findToolServer(toolName: string): string | null {
    for (const [serverName, client] of this.clients) {
      if (client.hasTool(toolName)) {
        return serverName;
      }
    }
    return null;
  }

  /**
   * Call a tool by name, automatically routing to the correct server.
   */
  async callTool(name: string, args: Record<string, unknown>): Promise<OperationResult> {
    const serverName = this.findToolServer(name);

    if (!serverName) {
      return {
        success: false,
        error: `Tool '${name}' not found in any registered server`,
      };
    }

    // Check if approval is needed based on autonomy level
    const approvalCheck = this.checkApprovalNeeded(name, args);
    if (approvalCheck.needsApproval) {
      return {
        success: false,
        requiresApproval: true,
        approvalReason: approvalCheck.reason,
      };
    }

    const client = this.clients.get(serverName)!;
    return client.callTool(name, args);
  }

  /**
   * Check if an operation needs approval based on autonomy level.
   */
  private checkApprovalNeeded(toolName: string, _args: Record<string, unknown>): { needsApproval: boolean; reason?: string } {
    // Tools that always require approval
    const securityCriticalTools = [
      'execute_command',
      'run_script',
      'delete_file',
      'git_push',
      'git_force_push',
      'docker_exec',
    ];

    // Tools that require approval at lower autonomy levels
    const writeTools = [
      'write_file',
      'edit_file',
      'create_file',
      'git_commit',
      'git_merge',
      'create_branch',
      'delete_branch',
    ];

    if (securityCriticalTools.includes(toolName)) {
      if (this.autonomyLevel < AutonomyLevel.FullAutonomy) {
        return {
          needsApproval: true,
          reason: `Security-critical operation '${toolName}' requires approval`,
        };
      }
    }

    if (writeTools.includes(toolName)) {
      if (this.autonomyLevel < AutonomyLevel.Balanced) {
        return {
          needsApproval: true,
          reason: `Write operation '${toolName}' requires approval at autonomy level ${this.autonomyLevel}`,
        };
      }
    }

    return { needsApproval: false };
  }

  /**
   * Request approval for a pending operation.
   */
  requestApproval(toolName: string, args: Record<string, unknown>, reason: string): string {
    const approvalId = `approval_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    this.pendingApprovals.set(approvalId, {
      tool: toolName,
      args,
      reason,
    });

    logger.info({ approvalId, tool: toolName }, 'Approval requested');

    return approvalId;
  }

  /**
   * Approve a pending operation.
   */
  async approveOperation(approvalId: string): Promise<OperationResult> {
    const pending = this.pendingApprovals.get(approvalId);

    if (!pending) {
      return {
        success: false,
        error: `No pending approval found with ID '${approvalId}'`,
      };
    }

    this.pendingApprovals.delete(approvalId);

    // Execute the approved operation
    logger.info({ approvalId, tool: pending.tool }, 'Operation approved, executing');

    return this.callToolUnsafe(pending.tool, pending.args);
  }

  /**
   * Reject a pending operation.
   */
  rejectOperation(approvalId: string): OperationResult {
    const pending = this.pendingApprovals.get(approvalId);

    if (!pending) {
      return {
        success: false,
        error: `No pending approval found with ID '${approvalId}'`,
      };
    }

    this.pendingApprovals.delete(approvalId);
    logger.info({ approvalId, tool: pending.tool }, 'Operation rejected');

    return {
      success: false,
      error: 'Operation rejected by user',
    };
  }

  /**
   * Get all pending approvals.
   */
  getPendingApprovals(): Array<{ id: string; tool: string; reason: string }> {
    const approvals: Array<{ id: string; tool: string; reason: string }> = [];

    for (const [id, pending] of this.pendingApprovals) {
      approvals.push({
        id,
        tool: pending.tool,
        reason: pending.reason,
      });
    }

    return approvals;
  }

  /**
   * Call a tool without approval checks (for approved operations).
   */
  private async callToolUnsafe(name: string, args: Record<string, unknown>): Promise<OperationResult> {
    const serverName = this.findToolServer(name);

    if (!serverName) {
      return {
        success: false,
        error: `Tool '${name}' not found in any registered server`,
      };
    }

    const client = this.clients.get(serverName)!;
    return client.callTool(name, args);
  }

  /**
   * Read a resource from a specific server.
   */
  async readResource(serverName: string, uri: string): Promise<OperationResult> {
    const client = this.clients.get(serverName);

    if (!client) {
      return {
        success: false,
        error: `Server '${serverName}' not registered`,
      };
    }

    return client.readResource(uri);
  }

  /**
   * Disconnect from all servers.
   */
  async disconnectAll(): Promise<void> {
    for (const [name, client] of this.clients) {
      await client.disconnect();
      logger.info({ server: name }, 'Disconnected');
    }

    this.clients.clear();
  }

  /**
   * Get list of connected servers.
   */
  getConnectedServers(): string[] {
    return Array.from(this.clients.keys());
  }

  /**
   * Update autonomy level.
   */
  setAutonomyLevel(level: AutonomyLevel): void {
    this.autonomyLevel = level;
    logger.info({ level }, 'Autonomy level updated');
  }
}
