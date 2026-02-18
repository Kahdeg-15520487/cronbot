import { AgentConfig, AgentStatus, Task } from './types.js';
/**
 * Main agent class that orchestrates task execution.
 */
export declare class Agent {
    private config;
    private stateManager;
    private mcpRegistry;
    private skillExecutor;
    private anthropic;
    private status;
    private currentTask;
    private shouldStop;
    private apiClient;
    constructor(config: AgentConfig);
    /**
     * Initialize the agent.
     */
    initialize(): Promise<void>;
    /**
     * Connect to system MCP servers.
     */
    private connectSystemMcps;
    /**
     * Start the agent's main loop.
     */
    start(): Promise<void>;
    /**
     * Stop the agent.
     */
    stop(): Promise<void>;
    /**
     * Get next task from Kanban service.
     */
    private getNextTask;
    /**
     * Execute a task using Claude.
     */
    private executeTask;
    /**
     * Build task context for Claude.
     */
    private buildTaskContext;
    /**
     * Build system prompt for Claude.
     */
    private buildSystemPrompt;
    /**
     * Get description of current autonomy level.
     */
    private getAutonomyDescription;
    /**
     * Build Claude tools from MCP tools.
     */
    private buildClaudeTools;
    /**
     * Process tool calls from Claude's response.
     */
    private processToolCalls;
    /**
     * Handle a detected blocker.
     */
    private handleBlocker;
    /**
     * Get current agent status.
     */
    getStatus(): AgentStatus;
    /**
     * Get current task.
     */
    getCurrentTask(): Task | null;
    /**
     * Sleep for a specified duration.
     */
    private sleep;
}
//# sourceMappingURL=agent.d.ts.map