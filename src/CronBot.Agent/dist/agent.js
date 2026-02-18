"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Agent = void 0;
const sdk_1 = __importDefault(require("@anthropic-ai/sdk"));
const types_js_1 = require("./types.js");
const manager_js_1 = require("./state/manager.js");
const registry_js_1 = require("./mcp/registry.js");
const executor_js_1 = require("./skills/executor.js");
const logger_js_1 = require("./logger.js");
const logger = (0, logger_js_1.createLogger)('agent');
/**
 * Main agent class that orchestrates task execution.
 */
class Agent {
    config;
    stateManager;
    mcpRegistry;
    skillExecutor;
    anthropic;
    status = types_js_1.AgentStatus.Idle;
    currentTask = null;
    shouldStop = false;
    apiClient;
    constructor(config) {
        this.config = config;
        this.stateManager = new manager_js_1.StateManager(config.statePath);
        this.mcpRegistry = new registry_js_1.McpRegistry(config.autonomyLevel);
        this.skillExecutor = new executor_js_1.SkillExecutor(config.skillsPath, config.workspacePath);
        this.anthropic = new sdk_1.default({
            apiKey: config.anthropicApiKey,
            baseURL: config.anthropicBaseUrl,
        });
        this.apiClient = new ApiClient(config.kanbanUrl);
    }
    /**
     * Initialize the agent.
     */
    async initialize() {
        logger.info({ agentId: this.config.agentId }, 'Initializing agent');
        // Initialize state manager
        await this.stateManager.initialize();
        // Initialize skills
        await this.skillExecutor.initialize();
        // Connect to system MCPs
        await this.connectSystemMcps();
        // Restore from checkpoint if available
        const checkpoint = await this.stateManager.getLatestCheckpoint();
        if (checkpoint) {
            logger.info({ checkpointId: checkpoint.id }, 'Restoring from checkpoint');
        }
        await this.stateManager.setPhase(types_js_1.AgentPhase.ReadingContext);
        logger.info('Agent initialized successfully');
    }
    /**
     * Connect to system MCP servers.
     */
    async connectSystemMcps() {
        // TODO: Load MCP configurations from registry service
        // For now, we'll skip MCP connections until the services are implemented
        logger.info('System MCP connections skipped (not yet implemented)');
    }
    /**
     * Start the agent's main loop.
     */
    async start() {
        logger.info('Starting agent main loop');
        this.shouldStop = false;
        this.status = types_js_1.AgentStatus.Working;
        while (!this.shouldStop) {
            try {
                // Check for blockers
                const blocker = this.stateManager.detectBlockers();
                if (blocker) {
                    await this.handleBlocker(blocker);
                    continue;
                }
                // Get next task
                const task = await this.getNextTask();
                if (!task) {
                    // No task available, wait and retry
                    await this.stateManager.setPhase(types_js_1.AgentPhase.ReadingContext);
                    await this.sleep(5000);
                    continue;
                }
                // Execute task
                await this.executeTask(task);
            }
            catch (error) {
                const message = error instanceof Error ? error.message : String(error);
                logger.error({ error: message }, 'Error in main loop');
                this.status = types_js_1.AgentStatus.Error;
                // Create checkpoint for recovery
                await this.stateManager.createCheckpoint();
                // Wait before retrying
                await this.sleep(10000);
                this.status = types_js_1.AgentStatus.Working;
            }
        }
        this.status = types_js_1.AgentStatus.Terminated;
        logger.info('Agent stopped');
    }
    /**
     * Stop the agent.
     */
    async stop() {
        logger.info('Stopping agent');
        this.shouldStop = true;
        // Create final checkpoint
        await this.stateManager.createCheckpoint();
        // Disconnect MCPs
        await this.mcpRegistry.disconnectAll();
    }
    /**
     * Get next task from Kanban service.
     */
    async getNextTask() {
        try {
            const response = await this.apiClient.get(`/projects/${this.config.projectId}/tasks/next`);
            if (response.success && response.data) {
                this.currentTask = response.data;
                return response.data;
            }
            return null;
        }
        catch (error) {
            logger.warn({ error }, 'Failed to get next task');
            return null;
        }
    }
    /**
     * Execute a task using Claude.
     */
    async executeTask(task) {
        logger.info({ taskId: task.id, title: task.title }, 'Executing task');
        await this.stateManager.setPhase(types_js_1.AgentPhase.Planning);
        await this.stateManager.recordDecision(`Started task: ${task.title}`, undefined, 5);
        // Update task status to in_progress
        await this.apiClient.patch(`/tasks/${task.id}`, { status: 'in_progress' });
        // Create checkpoint before starting work
        await this.stateManager.createCheckpoint();
        // Build context for Claude
        const context = this.buildTaskContext(task);
        // Get available tools
        const tools = this.buildClaudeTools();
        await this.stateManager.setPhase(types_js_1.AgentPhase.Executing);
        try {
            // Stream response from Claude
            const stream = this.anthropic.messages.stream({
                model: 'claude-opus-4-20250514',
                max_tokens: 4096,
                system: this.buildSystemPrompt(),
                messages: [
                    {
                        role: 'user',
                        content: context,
                    },
                ],
                tools,
            });
            // Wait for final message
            const message = await stream.finalMessage();
            // Process tool calls
            await this.processToolCalls(message);
            // Mark task as complete
            await this.stateManager.setPhase(types_js_1.AgentPhase.Verifying);
            await this.stateManager.recordDecision(`Completed task: ${task.title}`, undefined, 7);
            // Update task status
            await this.apiClient.patch(`/tasks/${task.id}`, { status: 'done' });
            logger.info({ taskId: task.id }, 'Task completed successfully');
        }
        catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            logger.error({ taskId: task.id, error: message }, 'Task execution failed');
            await this.stateManager.recordDecision(`Task failed: ${message}`, undefined, 3);
            // Update task status to blocked
            await this.apiClient.patch(`/tasks/${task.id}`, {
                status: 'blocked',
                blockedReason: message,
            });
            throw error;
        }
    }
    /**
     * Build task context for Claude.
     */
    buildTaskContext(task) {
        const context = this.stateManager.getContext();
        const recentDecisions = context.recentDecisions
            .slice(-5)
            .map(d => `- ${d.decision}`)
            .join('\n');
        return `
## Task Information
- **ID**: ${task.id}
- **Number**: #${task.number}
- **Title**: ${task.title}
- **Description**: ${task.description || 'No description provided'}
- **Type**: ${task.type}

## Recent Decisions
${recentDecisions || 'No recent decisions'}

## Available Tools
${this.mcpRegistry.getAllTools().map(t => `- ${t.tool.name}: ${t.tool.description}`).join('\n') || 'No tools available'}

## Available Skills
${this.skillExecutor.getAvailableSkills().map(s => `- ${s.meta.name}: ${s.meta.description}`).join('\n') || 'No skills available'}

Please complete this task. Start by creating a plan, then execute it step by step.
`;
    }
    /**
     * Build system prompt for Claude.
     */
    buildSystemPrompt() {
        const autonomyDescription = this.getAutonomyDescription();
        return `You are CronBot, an autonomous AI development assistant. You work on software development tasks autonomously.

## Autonomy Level: ${this.config.autonomyLevel} (${autonomyDescription})

## Guidelines
1. Always read and understand existing code before making changes
2. Make small, focused changes and verify after each change
3. Write clear commit messages describing what and why
4. If you encounter blockers, report them clearly
5. Document important decisions you make

## Available Actions
- Use MCP tools to interact with the filesystem, git, and other systems
- Execute skills for specialized operations
- Request approval for sensitive operations based on autonomy level

## Error Handling
- If an operation fails, try alternative approaches
- After 3 failures, report as blocked
- Create checkpoints before significant changes
`;
    }
    /**
     * Get description of current autonomy level.
     */
    getAutonomyDescription() {
        switch (this.config.autonomyLevel) {
            case types_js_1.AutonomyLevel.Reactive:
                return 'Read-only, responds when spoken to';
            case types_js_1.AutonomyLevel.Cautious:
                return 'Read/analyze, plans before executing, approval on writes';
            case types_js_1.AutonomyLevel.Balanced:
                return 'Autonomous on tasks, approval on major changes';
            case types_js_1.AutonomyLevel.FullAutonomy:
                return 'Full sandbox autonomy, only security-critical ops need approval';
            default:
                return 'Unknown';
        }
    }
    /**
     * Build Claude tools from MCP tools.
     */
    buildClaudeTools() {
        const tools = [];
        // Add MCP tools
        for (const { tool } of this.mcpRegistry.getAllTools()) {
            tools.push({
                name: tool.name,
                description: tool.description,
                input_schema: tool.inputSchema,
            });
        }
        // Add skill execution tool
        tools.push({
            name: 'execute_skill',
            description: 'Execute a Python skill script',
            input_schema: {
                type: 'object',
                properties: {
                    skill_name: {
                        type: 'string',
                        description: 'Name of the skill to execute',
                    },
                    arguments: {
                        type: 'object',
                        description: 'Arguments to pass to the skill',
                    },
                },
                required: ['skill_name'],
            },
        });
        return tools;
    }
    /**
     * Process tool calls from Claude's response.
     */
    async processToolCalls(message) {
        for (const block of message.content) {
            if (block.type === 'tool_use') {
                logger.info({ tool: block.name }, 'Processing tool call');
                let result;
                if (block.name === 'execute_skill') {
                    const args = block.input;
                    result = await this.skillExecutor.execute(args.skill_name, args.arguments || {});
                }
                else {
                    result = await this.mcpRegistry.callTool(block.name, block.input);
                }
                // Check if approval is needed
                if (result.requiresApproval) {
                    logger.info({ tool: block.name, reason: result.approvalReason }, 'Tool call requires approval');
                    // In a real implementation, this would wait for user approval
                    continue;
                }
                // Track failures for blocker detection
                if (!result.success) {
                    this.stateManager.trackToolFailure(block.name);
                }
                logger.debug({ tool: block.name, success: result.success }, 'Tool call completed');
            }
        }
    }
    /**
     * Handle a detected blocker.
     */
    async handleBlocker(blocker) {
        logger.warn({ blocker }, 'Blocker detected');
        this.status = types_js_1.AgentStatus.Blocked;
        await this.stateManager.setPhase(types_js_1.AgentPhase.Blocked);
        // Report blocker to API
        await this.apiClient.post(`/agents/${this.config.agentId}/blockers`, {
            type: blocker.type,
            severity: blocker.severity,
            description: blocker.description,
            suggestedAction: blocker.suggestedAction,
        });
        // Wait for resolution
        await this.sleep(30000);
        // Reset failure counters and retry
        this.stateManager.resetFailureCounters();
        this.status = types_js_1.AgentStatus.Working;
    }
    /**
     * Get current agent status.
     */
    getStatus() {
        return this.status;
    }
    /**
     * Get current task.
     */
    getCurrentTask() {
        return this.currentTask;
    }
    /**
     * Sleep for a specified duration.
     */
    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}
exports.Agent = Agent;
/**
 * Simple API client for communicating with CronBot services.
 */
class ApiClient {
    baseUrl;
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }
    async get(path) {
        try {
            const response = await fetch(`${this.baseUrl}${path}`);
            const data = await response.json();
            return { success: response.ok, data };
        }
        catch (error) {
            return { success: false, error: String(error) };
        }
    }
    async post(path, body) {
        try {
            const response = await fetch(`${this.baseUrl}${path}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            const data = await response.json();
            return { success: response.ok, data };
        }
        catch (error) {
            return { success: false, error: String(error) };
        }
    }
    async patch(path, body) {
        try {
            const response = await fetch(`${this.baseUrl}${path}`, {
                method: 'PATCH',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body),
            });
            const data = await response.json();
            return { success: response.ok, data };
        }
        catch (error) {
            return { success: false, error: String(error) };
        }
    }
}
//# sourceMappingURL=agent.js.map