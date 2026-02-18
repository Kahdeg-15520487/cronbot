"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.McpRegistry = void 0;
const client_js_1 = require("./client.js");
const types_js_1 = require("../types.js");
const logger_js_1 = require("../logger.js");
const logger = (0, logger_js_1.createLogger)('mcp-registry');
/**
 * Registry for managing MCP server connections.
 */
class McpRegistry {
    clients = new Map();
    autonomyLevel;
    pendingApprovals = new Map();
    constructor(autonomyLevel) {
        this.autonomyLevel = autonomyLevel;
    }
    /**
     * Register and connect to an MCP server.
     */
    async registerServer(name, config) {
        if (this.clients.has(name)) {
            logger.warn({ server: name }, 'Server already registered');
            return { success: true };
        }
        const client = new client_js_1.McpClient(config);
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
    async unregisterServer(name) {
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
    getAllTools() {
        const tools = [];
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
    findToolServer(toolName) {
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
    async callTool(name, args) {
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
        const client = this.clients.get(serverName);
        return client.callTool(name, args);
    }
    /**
     * Check if an operation needs approval based on autonomy level.
     */
    checkApprovalNeeded(toolName, _args) {
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
            if (this.autonomyLevel < types_js_1.AutonomyLevel.FullAutonomy) {
                return {
                    needsApproval: true,
                    reason: `Security-critical operation '${toolName}' requires approval`,
                };
            }
        }
        if (writeTools.includes(toolName)) {
            if (this.autonomyLevel < types_js_1.AutonomyLevel.Balanced) {
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
    requestApproval(toolName, args, reason) {
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
    async approveOperation(approvalId) {
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
    rejectOperation(approvalId) {
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
    getPendingApprovals() {
        const approvals = [];
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
    async callToolUnsafe(name, args) {
        const serverName = this.findToolServer(name);
        if (!serverName) {
            return {
                success: false,
                error: `Tool '${name}' not found in any registered server`,
            };
        }
        const client = this.clients.get(serverName);
        return client.callTool(name, args);
    }
    /**
     * Read a resource from a specific server.
     */
    async readResource(serverName, uri) {
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
    async disconnectAll() {
        for (const [name, client] of this.clients) {
            await client.disconnect();
            logger.info({ server: name }, 'Disconnected');
        }
        this.clients.clear();
    }
    /**
     * Get list of connected servers.
     */
    getConnectedServers() {
        return Array.from(this.clients.keys());
    }
    /**
     * Update autonomy level.
     */
    setAutonomyLevel(level) {
        this.autonomyLevel = level;
        logger.info({ level }, 'Autonomy level updated');
    }
}
exports.McpRegistry = McpRegistry;
//# sourceMappingURL=registry.js.map